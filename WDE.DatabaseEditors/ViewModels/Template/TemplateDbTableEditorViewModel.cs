﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.CustomCommands;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Expressions;
using WDE.DatabaseEditors.Extensions;
using WDE.DatabaseEditors.History;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.Solution;
using WDE.MVVM;
using WDE.MVVM.Observable;

namespace WDE.DatabaseEditors.ViewModels.Template
{
    public class TemplateDbTableEditorViewModel : ViewModelBase
    {
        private const string TipYouCanRevertId = "TableEditor.YouCanRevert";
    
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IMessageBoxService messageBoxService;
        private readonly IParameterFactory parameterFactory;
        private readonly IMySqlExecutor mySqlExecutor;
        private readonly IQueryGenerator queryGenerator;
        private readonly ITeachingTipService teachingTipService;
        private readonly ICreatureStatCalculatorService creatureStatCalculatorService;
        private readonly ISessionService sessionService;

        private readonly IDatabaseTableDataProvider tableDataProvider;
        
        public ObservableCollection<string> Header { get; } = new();
        public ReadOnlyObservableCollection<DatabaseRowsGroupViewModel> FilteredRows { get; }
        public IObservable<Func<DatabaseRowViewModel, bool>> CurrentFilter { get; }
        public SourceList<DatabaseRowViewModel> Rows { get; } = new();
        
        public AsyncAutoCommand<DatabaseCellViewModel?> RemoveTemplateCommand { get; }
        public AsyncAutoCommand<DatabaseCellViewModel?> RevertCommand { get; }
        public DelegateCommand<DatabaseCellViewModel?> SetNullCommand { get; }
        public AsyncAutoCommand<DatabaseCellViewModel> OpenParameterWindow { get; }
        private readonly Dictionary<string, ReactiveProperty<bool>> groupVisibilityByName = new();

        public AsyncAutoCommand AddNewCommand { get; }

        private bool canOpenRevertTip;
        public bool YouCanRevertTipOpened { get; set; }
        
        public TemplateDbTableEditorViewModel(DatabaseTableSolutionItem solutionItem,
            IDatabaseTableDataProvider tableDataProvider, IItemFromListProvider itemFromListProvider,
            IHistoryManager history, ITaskRunner taskRunner, IMessageBoxService messageBoxService,
            IEventAggregator eventAggregator, ISolutionManager solutionManager, 
            IParameterFactory parameterFactory, ISolutionTasksService solutionTasksService,
            ISolutionItemNameRegistry solutionItemName, IMySqlExecutor mySqlExecutor,
            IQueryGenerator queryGenerator, ITeachingTipService teachingTipService,
            ICreatureStatCalculatorService creatureStatCalculatorService,
            ITableDefinitionProvider tableDefinitionProvider,
            ISolutionItemIconRegistry iconRegistry, ISessionService sessionService,
            IDatabaseTableCommandService commandService) : base(history, solutionItem, solutionItemName, 
            solutionManager, solutionTasksService, eventAggregator, 
            queryGenerator, tableDataProvider, messageBoxService, taskRunner, parameterFactory, 
            tableDefinitionProvider, itemFromListProvider, iconRegistry, sessionService,
            commandService)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.tableDataProvider = tableDataProvider;
            this.messageBoxService = messageBoxService;
            this.parameterFactory = parameterFactory;
            this.mySqlExecutor = mySqlExecutor;
            this.queryGenerator = queryGenerator;
            this.teachingTipService = teachingTipService;
            this.creatureStatCalculatorService = creatureStatCalculatorService;
            this.sessionService = sessionService;

            OpenParameterWindow = new AsyncAutoCommand<DatabaseCellViewModel>(EditParameter);

            CurrentFilter = FunctionalExtensions.Select(this.ToObservable(t => t.SearchText), FilterItem);

            var comparer = Comparer<DatabaseRowsGroupViewModel>.Create((x, y) => x.GroupOrder.CompareTo(y.GroupOrder));
            AutoDispose(Rows.Connect()
                .Filter(CurrentFilter)
                .GroupOn(t => (t.CategoryName, t.CategoryIndex))
                .Transform(GroupCreate)
                .DisposeMany()
                .FilterOnObservable(t => t.ShowGroup)
                .Sort(comparer)
                .Bind(out ReadOnlyObservableCollection<DatabaseRowsGroupViewModel> filteredFields)
                .Subscribe(a =>
                {
                    
                }, b => throw b));
            FilteredRows = filteredFields;

            RemoveTemplateCommand = new AsyncAutoCommand<DatabaseCellViewModel?>(RemoveTemplate, vm => vm != null);
            RevertCommand = new AsyncAutoCommand<DatabaseCellViewModel?>(Revert, cell => cell is DatabaseCellViewModel vm && vm.CanBeReverted && vm.IsModified);
            SetNullCommand = new DelegateCommand<DatabaseCellViewModel?>(SetToNull, vm => vm != null && vm.CanBeSetToNull);
            AddNewCommand = new AsyncAutoCommand(AddNewEntity);

            canOpenRevertTip = !teachingTipService.IsTipShown(TipYouCanRevertId);
            
            ScheduleLoading();
        }

        protected override void UpdateSolutionItem()
        {
            solutionItem.Entries = Entities.Select(e =>
                new SolutionItemDatabaseEntity(e.Key, e.ExistInDatabase, GetOriginalFields(e))).ToList();
        }

        private async Task AddNewEntity()
        {
            var parameter = parameterFactory.Factory(tableDefinition.Picker);
            var selected = await itemFromListProvider.GetItemFromList(parameter.Items, false);
            if (!selected.HasValue)
                return;

            var data = await tableDataProvider.Load(tableDefinition.Id, (uint) selected);
            if (data == null) 
                return;

            foreach (var entity in data.Entities)
            {
                if (ContainsEntity(entity))
                {
                    await messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Entity already added")
                        .SetMainInstruction($"Entity {entity.Key} is already added to the editor")
                        .WithOkButton(false)
                        .SetIcon(MessageBoxIcon.Information)
                        .Build());
                    continue;
                }
                if (!entity.ExistInDatabase)
                {
                    if (!await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                        .SetTitle("Entity doesn't exist in database")
                        .SetMainInstruction($"Entity {entity.Key} doesn't exist in the database")
                        .SetContent(
                            "WoW Database Editor will be generating DELETE/INSERT query instead of UPDATE. Do you want to continue?")
                        .WithYesButton(true)
                        .WithNoButton(false).Build()))
                        continue;
                }
                await AddEntity(entity);
            }
        }
        
        private void SetToNull(DatabaseCellViewModel? view)
        {
            if (view != null && view.CanBeNull && !view.Parent.IsReadOnly) 
                view.ParameterValue!.SetNull();
        }

        private async Task Revert(DatabaseCellViewModel? view)
        {
            if (view == null || view.Parent.IsReadOnly || view.TableField == null)
                return;
            
            view.ParameterValue!.Revert();
            
            if (!view.ParentEntity.ExistInDatabase)
                return;
            
            if (!mySqlExecutor.IsConnected)
                return;

            if (!await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Reverting")
                .SetMainInstruction("Do you want to revert field in the database?")
                .SetContent(
                    "Reverted field will become unmodified field and unmodified fields are not generated in query. Therefore if you want to revert the field in the database, it can be done now.\n\nDo you want to revert the field in the database now (this will execute query)?")
                .SetIcon(MessageBoxIcon.Information)
                .WithYesButton(true)
                .WithNoButton(false)
                .Build()))
                return;

            var query = queryGenerator.GenerateUpdateFieldQuery(tableDefinition, view.ParentEntity, view.TableField);
            await mySqlExecutor.ExecuteSql(query);
        }

        private async Task RemoveTemplate(DatabaseCellViewModel? view)
        {
            if (view == null)
                return;

            await RemoveEntity(view.ParentEntity);
        }

        private DatabaseRowsGroupViewModel GroupCreate(IGroup<DatabaseRowViewModel, (string CategoryName, int CategoryIndex)> @group)
        {
            return new (@group, GetGroupVisibility(@group.GroupKey.CategoryName));
        }

        private Func<DatabaseRowViewModel, bool> FilterItem(string text)
        {
            if (string.IsNullOrEmpty(text)) 
                return _ => true;
            var lower = text.ToLower();
            if (lower.Contains("||"))
            {
                var parts = lower.Split("||").Select(a => a.Trim()).ToList();
                return item =>
                {
                    foreach (var p in parts)
                        if (item.Name.ToLower().Contains(p))
                            return true;
                    return false;
                };
            }
            else
                return item => item.Name.ToLower().Contains(lower);
        }
        
        private bool ContainsEntity(DatabaseEntity entity)
        {
            foreach (var e in Entities)
                if (e.Key == entity.Key)
                    return true;
            return false;
        }

        private Task EditParameter(DatabaseCellViewModel cell) => EditParameter(cell.ParameterValue!);

        protected override List<EntityOrigianlField>? GetOriginalFields(DatabaseEntity entity)
        {
            var modified = entity.Fields.Where(f => f.IsModified).ToList();
            if (modified.Count == 0)
                return null;
            
            return modified.Select(f => new EntityOrigianlField()
                {ColumnName = f.FieldName, OriginalValue = f.OriginalValue}).ToList();
        }

        public override DatabaseEntity AddRow(uint key)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RemoveEntity(DatabaseEntity entity)
        {
            if (!await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Delete entity")
                .SetMainInstruction($"Do you want to delete entity with key {entity.Key} from the editor?")
                .SetContent(
                    "It will be removed only from the project editor, it will not be removed from the database.")
                .WithYesButton(true)
                .WithNoButton(false).Build()))
                return false;

            return ForceRemoveEntity(entity);
        }

        public override bool ForceRemoveEntity(DatabaseEntity entity)
        {
            var indexOfEntity = Entities.IndexOf(entity);
            if (indexOfEntity == -1)
                return false;
            
            Entities.RemoveAt(indexOfEntity);
            Header.RemoveAt(indexOfEntity);
            foreach (var row in Rows.Items)
                row.Cells.RemoveAt(indexOfEntity);

            ReEvalVisibility();
            
            return true;
        }
        
        public Task<bool> AddEntity(DatabaseEntity entity)
        {
            return Task.FromResult(ForceInsertEntity(entity, Entities.Count));
        }

        public override bool ForceInsertEntity(DatabaseEntity entity, int index)
        {
            Dictionary<string, IObservable<bool>?> groupVisibility = new();

            var pseudoItem = new DatabaseTableSolutionItem(entity.Key, entity.ExistInDatabase, tableDefinition.Id);
            var savedItem = sessionService.Find(pseudoItem);
            if (savedItem is DatabaseTableSolutionItem savedTableItem)
                savedTableItem.UpdateEntitiesWithOriginalValues(new List<DatabaseEntity>(){entity});
            
            foreach (var row in Rows.Items)
            {
                var column = row.ColumnData;
                DatabaseCellViewModel cellViewModel;
                
                if (column.IsMetaColumn)
                {
                    var evaluator = new DatabaseExpressionEvaluator(creatureStatCalculatorService, parameterFactory, tableDefinition, column.Expression!);
                    var parameterValue = new ParameterValue<string>(new ValueHolder<string>(evaluator.Evaluate(entity)!.ToString(), false),
                        new ValueHolder<string>("", false), StringParameter.Instance);
                    entity.OnAction += _ => parameterValue.Value = evaluator.Evaluate(entity)!.ToString();
                    cellViewModel = AutoDispose(new DatabaseCellViewModel(row, entity, parameterValue));
                }
                else
                {
                    var cell = entity.GetCell(column.DbColumnName);
                    if (cell == null)
                        throw new Exception("this should never happen");
                            
                    IParameterValue parameterValue = null!;
                    if (cell is DatabaseField<long> longParam)
                    {
                        parameterValue = new ParameterValue<long>(longParam.Current, longParam.Original, parameterFactory.Factory(column.ValueType));
                    }
                    else if (cell is DatabaseField<string> stringParam)
                    {
                        parameterValue = new ParameterValue<string>(stringParam.Current, stringParam.Original, parameterFactory.FactoryString(column.ValueType));
                    }
                    else if (cell is DatabaseField<float> floatParameter)
                    {
                        parameterValue = new ParameterValue<float>(floatParameter.Current, floatParameter.Original, FloatParameter.Instance);
                    }

                    IObservable<bool>? cellVisible = null!;
                    var group = row.GroupData;
                    if (group.ShowIf.HasValue)
                    {
                        if (!groupVisibility.TryGetValue(group.Name, out cellVisible))
                        {
                            var compareCell = entity.GetCell(group.ShowIf.Value.ColumnName);
                            if (compareCell != null && compareCell is DatabaseField<long> lField)
                            {
                                var comparedValue = group.ShowIf.Value.Value;
                                cellVisible = Observable.Select(lField.Current.ToObservable(p => p.Value), val => val == comparedValue);
                            }
                            groupVisibility[group.Name] = cellVisible;
                        }
                    }

                    cellViewModel = AutoDispose(new DatabaseCellViewModel(row, entity, cell, parameterValue, cellVisible));
                }

                
                row.Cells.Insert(index, cellViewModel);
            }

            Entities.Insert(index, entity);
            var name = parameterFactory.Factory(tableDefinition.Picker).ToString(entity.Key);
            Header.Insert(index, name);

            var typeCell = entity.GetCell("type");
            if (typeCell == null)
                return true;
            typeCell.PropertyChanged += (_, _) => ReEvalVisibility();

            ReEvalVisibility();
            
            return true;
        }

        protected override ICollection<uint> GenerateKeys() => Entities.Select(e => e.Key).ToList();

        protected override async Task InternalLoadData(DatabaseTableData data)
        {
            Rows.Clear();
            Header.Clear();
            groupVisibilityByName.Clear();

            int categoryIndex = 0;
            int columnIndex = 0;

            foreach (var group in tableDefinition.Groups)
            {
                categoryIndex++;
                groupVisibilityByName[group.Name] = new ReactiveProperty<bool>(true);

                foreach (var column in group.Fields)
                {
                    var row = new DatabaseRowViewModel(column, group, categoryIndex, columnIndex++);
                    if (canOpenRevertTip)
                        row.FieldModified += RowOnFieldModified;
                    Rows.Add(row);
                }
            }

            await AsyncAddEntities(data.Entities);
            
            historyHandler = History.AddHandler(AutoDispose(new TemplateTableEditorHistoryHandler(this)));
        }

        private TemplateTableEditorHistoryHandler? historyHandler;
        protected override IDisposable BulkEdit(string name) => historyHandler?.BulkEdit(name) ?? Disposable.Empty;
        
        private void RowOnFieldModified()
        {
            if (teachingTipService.ShowTip(TipYouCanRevertId))
            {
                YouCanRevertTipOpened = true;
                RaisePropertyChanged(nameof(YouCanRevertTipOpened));
            }
        }

        private async Task AsyncAddEntities(IList<DatabaseEntity> tableDataEntities)
        {
            List<DatabaseEntity> finalList = new();
            foreach (var entity in tableDataEntities)
            {
                if (await AddEntity(entity))
                    finalList.Add(entity);
            }

            ReEvalVisibility();
        }

        private void ReEvalVisibility()
        {
            foreach (var group in tableDefinition.Groups)
            {
                if (!group.ShowIf.HasValue)
                    continue;

                groupVisibilityByName[group.Name].Value = false;
                foreach (var entity in Entities)
                {
                    var cell = entity.GetCell(group.ShowIf.Value.ColumnName);
                    if (cell is not DatabaseField<long> lField)
                        continue;
                    if (lField.Current.Value == group.ShowIf.Value.Value)
                    {
                        groupVisibilityByName[group.Name].Value = true;
                        break;
                    }
                }
            }
        }

        public IObservable<bool> GetGroupVisibility(string str)
        {
            return groupVisibilityByName[str];
        }
        
        private string searchText = "";
        public string SearchText
        {
            get => searchText;
            set => SetProperty(ref searchText, value);
        }
    }
}
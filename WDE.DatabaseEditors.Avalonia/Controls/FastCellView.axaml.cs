using System;
using System.Globalization;
using System.Reactive.Disposables;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;

namespace WDE.DatabaseEditors.Avalonia.Controls
{
    public class FastCellView : OpenableFastCellViewBase
    {
        private bool showChooseButton;
        public static readonly DirectProperty<FastCellView, bool> ShowChooseButtonProperty = AvaloniaProperty.RegisterDirect<FastCellView, bool>("ShowChooseButton", o => o.ShowChooseButton, (o, v) => o.ShowChooseButton = v);
        private ICommand? chooseParameterCommand;
        public static readonly DirectProperty<FastCellView, ICommand?> ChooseParameterCommandProperty = AvaloniaProperty.RegisterDirect<FastCellView, ICommand?>("ChooseParameterCommand", o => o.ChooseParameterCommand, (o, v) => o.ChooseParameterCommand = v);
        
        public ICommand? ChooseParameterCommand
        {
            get => chooseParameterCommand;
            set => SetAndRaise(ChooseParameterCommandProperty, ref chooseParameterCommand, value);
        }

        public bool ShowChooseButton
        {
            get => showChooseButton;
            set => SetAndRaise(ShowChooseButtonProperty, ref showChooseButton, value);
        }

        private TextBox? textBox;
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            partPanel = e.NameScope.Find<Panel>("PART_Panel");
            partText = e.NameScope.Find<TextBlock>("PART_text");
            
            if (!isReadOnly)
                partText.Cursor = new Cursor(StandardCursorType.Ibeam);
            
            if (showChooseButton && !isReadOnly)
            {
                var chooseButton = new Button()
                {
                    Content = "...",
                    Focusable = false,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Width = 30,
                    Command = ChooseParameterCommand,
                    CommandParameter = DataContext
                };
                    
                partPanel.Children.Add(chooseButton);
            }
        }

        protected override Control CreateEditingControl()
        {
            textBox = new TextBox()
            {
                FontFamily = GetValue(FontFamilyProperty)
            };
            textBox.Classes.Add("GridViewPlainTextBox");
            textBox.DataContext = this;
            textBox.Bind(TextBox.TextProperty, new Binding("Value", BindingMode.OneTime));
            textBox.MinWidth = 0;
            textBox.MinHeight = 0;
            textBox.Margin = new Thickness(partText?.Margin.Left ?? 0,partText?.Margin.Top ?? 0, 0, 0);
            textBox.Padding = new Thickness(partText?.Padding.Left ?? 0,partText?.Padding.Top ?? 0, 0, 0);
            var disposable1 = textBox.AddDisposableHandler(KeyDownEvent, (sender, args) =>
            {
                HandleMoveLeftRightUpBottom(args, false);
                if (args.Key is Key.Return or Key.Enter)
                {
                    EndEditing();
                    args.Handled = true;
                }
                else if (args.Key == Key.Escape)
                {
                    EndEditing(false);
                    args.Handled = true;
                }
            });
            var disposable2 = textBox.AddDisposableHandler(LostFocusEvent, (sender, args) =>
            {
                EndEditing();
            });
            textBoxDisposable = new CompositeDisposable(disposable1, disposable2);
            textBox.SelectAll();
            return textBox;
        }

        protected override void EndEditingInternal(bool commit)
        {
            if (textBox != null && commit)
            {
                if (Value is long)
                {
                    if (long.TryParse(textBox.Text, out var value))
                        Value = value;
                }
                else if (Value is float)
                {
                    if (float.TryParse(textBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                        Value = value;
                }
                else
                    Value = textBox.Text;
            }

            textBox = null;
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            if (!OpenForEditing())
                return;
            e.Handled = true;
            if (e.Text != "\n" && e.Text != " " && e.Text != "\r")
            {
                DispatcherTimer.RunOnce(() =>
                {
                    if (textBox == null)
                        return;
                    textBox.RaiseEvent(new TextInputEventArgs
                    {
                        Device = e.Device,
                        Handled = false,
                        Text = e.Text,
                        Route = e.Route,
                        RoutedEvent = e.RoutedEvent,
                        Source = textBox
                    });
                }, TimeSpan.FromMilliseconds(1));
            }
        }
        
        protected override void PasteImpl(string text)
        {
            if (isReadOnly)
                return;
            
            Value = text;
        }

        public override void DoCopy(IClipboard clipboard)
        {
            clipboard.SetTextAsync(Value.ToString()!);
        }
    }
}
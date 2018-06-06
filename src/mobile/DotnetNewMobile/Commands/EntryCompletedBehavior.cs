using System;
using System.Windows.Input;
using Xamarin.Forms;

namespace DotnetNewMobile
{
    public class EntryCompletedBehavior : Behavior<Entry>
    {
        public static readonly BindableProperty CommandProperty = 
            BindableProperty.Create ("Command", typeof(ICommand), typeof(EntryCompletedBehavior), null);

        public static readonly BindableProperty EntryConverterProperty =
            BindableProperty.Create("Converter", typeof(IValueConverter), typeof(EntryCompletedBehavior), null);
    
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public IValueConverter Converter
        {
            get { return (IValueConverter)GetValue(EntryConverterProperty); }
            set { SetValue(EntryConverterProperty, value); }
        }

        public Entry AssociatedObject { get; private set; }

        protected override void OnAttachedTo(Entry bindable)
        {
            base.OnAttachedTo(bindable);
            AssociatedObject = bindable;
            bindable.BindingContextChanged += OnBindingContextChanged;
            bindable.Completed += OnEntryCompleted;
        }

        protected override void OnDetachingFrom(Entry bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.BindingContextChanged -= OnBindingContextChanged;
            bindable.Completed -= OnEntryCompleted;

            AssociatedObject = null;
        }

        void OnBindingContextChanged(object sender, EventArgs e)
        {
            OnBindingContextChanged();
        }

        void OnEntryCompleted(object sender, EventArgs e)
        {
            if (Command == null)
            {
                return;
            }

            object parameter = Converter.Convert(e, typeof(object), null, null);
            if (Command.CanExecute(parameter))
            {
                Command.Execute(parameter);
            }
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            BindingContext = AssociatedObject.BindingContext;
        }
    }
}

using System;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using DotnetNewMobile.Controls;

[assembly: ExportRenderer(typeof(LabelWithCopyOnLongPress), typeof(LabelWithCopyOnLongPressRenderer))]
namespace DotnetNewMobile.Controls
{
    public class LabelWithCopyOnLongPressRenderer : LabelRenderer
    {
        LabelWithCopyOnLongPress label;

        public LabelWithCopyOnLongPressRenderer()
        {
            this.AddGestureRecognizer(new UILongPressGestureRecognizer((longPress) => {
                if (longPress.State == UIGestureRecognizerState.Began)
                {
                    label.HandleLongPress();
                }
            }));
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Label> e)
        {
            base.OnElementChanged(e);

            if(e != null && e.NewElement != null){
                label = e.NewElement as LabelWithCopyOnLongPress;
            }
        }
    }
}

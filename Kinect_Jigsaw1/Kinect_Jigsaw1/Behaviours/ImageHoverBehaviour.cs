using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;

namespace Kinect_Jigsaw1.Behaviours
{
    public class ImageHoverBehaviour : Behavior<Button>
    {
        static readonly string CanvasName = "HoverButtonBehaviorPanel";
        static readonly string ProgressIndiciatorName = "HoverProgressIndiciator";

        bool hovering;
        Canvas canvas;
        HoverProgressControl hoverProgressControl;

        protected override void OnAttached()
        {
            base.OnAttached();

            if (this.AssociatedObject.Parent is Panel)
            {
                this.AssociatedObject.MouseEnter += new MouseEventHandler(AssociatedObject_MouseEnter);
                this.AssociatedObject.MouseLeave += new MouseEventHandler(AssociatedObject_MouseLeave);
                this.AssociatedObject.MouseMove += new MouseEventHandler(AssociatedObject_MouseMove);
                this.SetUpCanvas();
                this.SetUpHoverAnimationControl();
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (this.AssociatedObject.Parent is Panel)
            {
                this.AssociatedObject.MouseEnter -= AssociatedObject_MouseEnter;
                this.AssociatedObject.MouseLeave -= AssociatedObject_MouseLeave;
                this.AssociatedObject.MouseMove -= AssociatedObject_MouseMove;
                this.hoverProgressControl.Storyboard.Completed -= Storyboard_Completed;

                var panel = (Panel)this.AssociatedObject.Parent;
                canvas.Children.Remove(this.hoverProgressControl);
                panel.Children.Remove(canvas);

                this.hoverProgressControl = null;
                this.canvas = null;
            }
        }

        void AssociatedObject_MouseLeave(object sender, MouseEventArgs e)
        {
            this.hovering = false;
            this.hoverProgressControl.Visibility = Visibility.Collapsed;
            this.hoverProgressControl.Storyboard.Stop();
        }

        void AssociatedObject_MouseEnter(object sender, MouseEventArgs e)
        {
            this.hovering = true;
            this.hoverProgressControl.Visibility = Visibility.Visible;
            this.hoverProgressControl.Storyboard.Begin();
        }

        void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.hovering)
            {
                var point = Mouse.GetPosition(this.canvas);
                this.hoverProgressControl.SetValue(Canvas.LeftProperty, point.X);
                this.hoverProgressControl.SetValue(Canvas.TopProperty, point.Y);
            }
        }

        void Storyboard_Completed(object sender, EventArgs e)
        {
            this.hovering = false;
            this.hoverProgressControl.Visibility = Visibility.Collapsed;
            if (this.AssociatedObject.Command != null
                && this.AssociatedObject.Command.CanExecute(null))
                this.AssociatedObject.Command.Execute(
                    this.AssociatedObject.CommandParameter);
        }

        void SetUpCanvas()
        {
            var panel = (Panel)this.AssociatedObject.Parent;

            // re-use an existing canvas if it is found
            var existingCanvas = panel.FindName(CanvasName);
            if (existingCanvas != null)
            {
                this.canvas = (Canvas)existingCanvas;
            }
            else
            {
                this.canvas = new Canvas();
                this.canvas.Name = CanvasName;
                panel.Children.Add(canvas);
            }
        }

        void SetUpHoverAnimationControl()
        {
            // re-use an existing hover control if it is found
            var existingHoverControl = this.canvas.FindName(ProgressIndiciatorName);
            if (existingHoverControl != null)
            {
                this.hoverProgressControl = (HoverProgressControl)existingHoverControl;
            }
            else
            {
                this.hoverProgressControl = new HoverProgressControl();
                this.hoverProgressControl.Name = ProgressIndiciatorName;
                this.hoverProgressControl.Visibility = Visibility.Collapsed;
            }
            this.hoverProgressControl.Storyboard.Completed += new EventHandler(Storyboard_Completed);
            this.canvas.Children.Add(this.hoverProgressControl);
        }
    }
}

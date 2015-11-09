using System.Windows.Input;

namespace Photo_Editor
{
    public static class CustomCommands
    {
        public static readonly RoutedUICommand Exit = new RoutedUICommand
                (
                        "Exit",
                        "Exit",
                        typeof(CustomCommands),
                        new InputGestureCollection()
                        {
                                        new KeyGesture(Key.F4, ModifierKeys.Alt)
                        }
                );

        public static readonly RoutedUICommand Open = new RoutedUICommand
            (
                    "Open",
                    "Open",
                    typeof(CustomCommands),
                    new InputGestureCollection()
                    {
                        new KeyGesture(Key.O, ModifierKeys.Control)
                    }
            );

        public static readonly RoutedUICommand Save = new RoutedUICommand
            (
                    "Save",
                    "Save",
                    typeof(CustomCommands),
                    new InputGestureCollection()
                    {
                        new KeyGesture(Key.S, ModifierKeys.Control)
                    }
            );

        public static readonly RoutedUICommand Undo = new RoutedUICommand
            (
                    "Undo",
                    "Undo",
                    typeof(CustomCommands),
                    new InputGestureCollection()
                    {
                        new KeyGesture(Key.U, ModifierKeys.Control)
                    }
            );
        public static readonly RoutedUICommand Redo = new RoutedUICommand
            (
                    "Redo",
                    "Redo",
                    typeof(CustomCommands),
                    new InputGestureCollection()
                    {
                        new KeyGesture(Key.R, ModifierKeys.Control)
                    }
            );
        public static readonly RoutedUICommand Resize = new RoutedUICommand
            (
                    "Resize",
                    "Resize",
                    typeof(CustomCommands),
                    new InputGestureCollection()
                    {
                        new KeyGesture(Key.R, ModifierKeys.Control)
                    }
            );
        public static readonly RoutedUICommand ApplyResize = new RoutedUICommand
            (
                    "Resize",
                    "Resize",
                    typeof(CustomCommands),
                    new InputGestureCollection()
                    {
                        new KeyGesture(Key.R, ModifierKeys.Control)
                    }
            );
        //Define more commands here, just like the one above
    }
}
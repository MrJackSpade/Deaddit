﻿namespace Deaddit.MAUI.EventArguments
{
    public class ObjectEditorSaveEventArgs(object saved) : EventArgs
    {
        public object Saved { get; private set; } = saved;
    }
}
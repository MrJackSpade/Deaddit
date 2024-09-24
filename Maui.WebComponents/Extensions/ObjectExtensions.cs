using System.Reflection;

namespace Maui.WebComponents.Extensions
{
    internal static class ObjectExtensions
    {
        public static void RemoveEventHandlers(this object target, string eventName)
        {
            Type? type = target.GetType();
            EventInfo eventInfo = null;

            // Try to get the event, including non-public events
            while (type != null)
            {
                eventInfo = type.GetEvent(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (eventInfo != null)
                {
                    break;
                }

                type = type.BaseType;
            }

            if (eventInfo != null)
            {
                // Get the backing field of the event
                FieldInfo? fieldInfo = type.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (fieldInfo == null)
                {
                    // For auto-implemented events, the field name could be different
                    fieldInfo = type.GetField($"<{eventName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                }

                if (fieldInfo != null)
                {
                    // Set the field to null to remove all event handlers
                    fieldInfo.SetValue(target, null);
                }
                else
                {
                    // As a last resort, remove handlers via the remove method
                    MethodInfo? removeMethod = eventInfo.GetRemoveMethod(true);
                    if (removeMethod != null)
                    {
                        FieldInfo? delegateField = type.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                        if (delegateField != null)
                        {
                            if (delegateField.GetValue(target) is MulticastDelegate eventDelegate)
                            {
                                foreach (Delegate d in eventDelegate.GetInvocationList())
                                {
                                    removeMethod.Invoke(target, new object[] { d });
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

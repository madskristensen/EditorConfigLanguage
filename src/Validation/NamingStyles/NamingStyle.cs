using System;

namespace EditorConfig.Validation.NamingStyles
{
    internal readonly struct NamingStyle
    {
        public NamingStyle(Guid id)
        {
            ID = id;
        }

        public Guid ID { get; }
    }
}

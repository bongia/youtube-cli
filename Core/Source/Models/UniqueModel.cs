using System;

namespace MasDev.YouTube.Model
{
    /// <summary>
    ///  Represents an object that can be dentified with a GeneratedId
    /// </summary>
    public class UniqueModel
    {
        public readonly Guid GeneratedId = Guid.NewGuid();
    }
}
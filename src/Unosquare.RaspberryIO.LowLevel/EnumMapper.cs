using System;
using System.Collections.Generic;
using System.Globalization;

namespace Unosquare.RaspberryIO.LowLevel
{
    /// <summary>
    /// Bidirectional mappings between two enumerations.
    /// </summary>
    [Serializable]
    public class EnumMapper<TE1, TE2>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumMapper{TE1, TE2}"/> class.
        /// Constructor.
        /// </summary>
        public EnumMapper()
        {
            MappingFirstToSecond = new Dictionary<TE1, TE2>();
            MappingSecondToFirst = new Dictionary<TE2, TE1>();
            HasDefaultValue1 = false;
            HasDefaultValue2 = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumMapper{TE1, TE2}"/> class.
        /// Constructor with default mapping values.
        /// </summary>
        public EnumMapper(TE1 defaultValue1, TE2 defaultValue2)
            : this()
        {
            HasDefaultValue1 = true;
            HasDefaultValue2 = true;
            DefaultValue1 = defaultValue1;
            DefaultValue2 = defaultValue2;
        }

        protected IDictionary<TE1, TE2> MappingFirstToSecond
        {
            get;
        }

        protected IDictionary<TE2, TE1> MappingSecondToFirst
        {
            get;
        }

        protected TE1 DefaultValue1
        {
            get;
            set;
        }

        protected TE2 DefaultValue2
        {
            get;
            set;
        }

        protected bool HasDefaultValue1
        {
            get;
            set;
        }

        protected bool HasDefaultValue2
        {
            get;
            set;
        }

        public int Count
        {
            get
            {
                return MappingFirstToSecond.Count;
            }
        }

        /// <summary>
        /// Gets the mapping value for the second enum.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when argument is not defined</exception>
        public virtual TE2 Get(TE1 v1)
        {
            TE2 v2;
            if (MappingFirstToSecond.TryGetValue(v1, out v2))
            {
                return v2;
            }
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} is not defined", v1), "v1");
        }

        /// <summary>
        /// Gets the mapping value for the second enum.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when argument is not defined</exception>
        public virtual TE1 Get(TE2 v2)
        {
            TE1 v1;
            if (MappingSecondToFirst.TryGetValue(v2, out v1))
            {
                return v1;
            }
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} is not defined", v2), "v2");
        }

        /// <summary>
        /// Gets the mapping value for the second enum.
        /// </summary>
        public virtual bool Get(TE1 v1, out TE2 v2)
        {
#pragma warning disable SA1305 // Field names should not use Hungarian notation
            bool bMapped = MappingFirstToSecond.TryGetValue(v1, out v2);
#pragma warning restore SA1305 // Field names should not use Hungarian notation
            if (!bMapped && HasDefaultValue2)
            {
                v2 = DefaultValue2;
                bMapped = true;
            }
            return bMapped;
        }

        /// <summary>
        /// Gets the mapping value for the first enum.
        /// </summary>
        public virtual bool Get(TE2 v2, out TE1 v1)
        {
#pragma warning disable SA1305 // Field names should not use Hungarian notation
            bool bMapped = MappingSecondToFirst.TryGetValue(v2, out v1);
#pragma warning restore SA1305 // Field names should not use Hungarian notation
            if (!bMapped && HasDefaultValue1)
            {
                v1 = DefaultValue1;
                bMapped = true;
            }
            return bMapped;
        }

        /// <summary>
        /// Defines a bidirectional mapping.
        /// An exception is thrown when a mapping is redefined.
        /// </summary>
        public virtual void Add(TE1 v1, TE2 v2)
        {
            // TODO: Add lock if there is another method which manipulates the collections
            MappingFirstToSecond.Add(v1, v2);
            MappingSecondToFirst.Add(v2, v1);
        }
    }
}

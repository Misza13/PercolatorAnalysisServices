﻿/*  
 * Percolator Analysis Services
 *  Copyright (c) 2014 CoopDIGITy
 *  Author: Matthew Hallmark
 *  A Copy of the Liscence is included in the "AssemblyInfo.cs" file.
 */

namespace Percolator.AnalysisServices.Linq
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using Percolator.AnalysisServices.Attributes;

    /// <summary>
    /// Representation of a MDX 'Set'.
    /// </summary>
    public class Set : ICubeObject
    {
        protected List<object> _values;

        /// <summary>
        /// Representation of a MDX 'Set'.
        /// </summary>
        /// <param name="objs">The cube objects to assemble the set.</param>
        public Set(params ICubeObject[] objs)
        {
            this._values = new List<object>(objs);
        }

        /// <summary>
        /// Representation of a MDX 'Set'.
        /// </summary>
        /// <param name="objs">The cube objects to assemble the set.</param>
        public Set(params string[] objs)
        {
            this._values = new List<object>(objs);
        }

        /// <summary>
        /// Representation of a MDX 'Set'.
        /// </summary>
        /// <param name="obj">String representation of a set.</param>
        public Set(string obj)
        {
            this._values = new List<object> { obj };
        }

        /// <summary>
        /// The type of the Value property.
        /// </summary>
        public Type ValueType { get; protected set; }

        /// <summary>
        /// Returns the number of cells in a set.
        /// </summary>
        public Member Count => this.assembleExtension("Count");

        /// <summary>
        /// Returns the set of children of a specified member.
        /// </summary>
        public Set Children => this.assembleExtension("Children");

        /// <summary>
        /// Returns the current tuple from a set during iteration.
        /// </summary>
        public Member Current => this.assembleExtension("Current");

        /// <summary>
        /// Returns the current member along a specified hierarchy during iteration.
        /// </summary>
        public Member CurrentMember => this.assembleExtension("CurrentMember");

        /// <summary>
        /// Returns the current iteration number within a set during iteration.
        /// </summary>
        public Member CurrentOrdinal => this.assembleExtension("CurrentOrdinal");

        /// <summary>
        /// Returns the set of members in a dimension, level, or hierarchy.
        /// </summary>
        public Set Members => this.assembleExtension("Members");

        /// <summary>
        /// Returns the hierarchy that contains a specified member, level, or hierarchy.
        /// </summary>
        public Member Dimension => this.assembleExtension("Dimension");

        /// <summary>
        /// The named of the Set.
        /// </summary>
        public string Tag { get; set; }

        public static implicit operator string(Set set) => set.ToString();

        public static implicit operator bool(Set set) => true;

        public static implicit operator Set(string str) => new Set(str);

        public static Set operator *(Set set1, Set set2) => $"{set1} * {set2}";

        public static Set operator *(Set set, Member member) => $"{set} * {member}";

        public static Set operator *(Member member, Set set) => $"{set} * {member}";

        public static Set operator &(Measure measure, Set set) => $"({measure}, {set})";

        public static Set operator &(Set set, Measure measure) => $"({set}, {measure})";

        public static Set operator &(Set set1, Set set2) => $"({set1}, {set2})";

        public static Set operator &(Member member, Set set) => $"({member}, {set})";

        public static Set operator &(Set set, Member member) => $"({set}, {member})";

        /// <summary>
        /// Factory method to create a new set based on the expression passed in.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="setCreator"></param>
        /// <returns></returns>
        public static Set Create<T>(Func<T, Set> setCreator) => setCreator(typeof(T).GetCubeInstance<T>());

        public Member Item(int itemNumber) => $"{this.assembleSet()}.Item({itemNumber})";

        /// <summary>
        /// Returns the MDX syntax for this set.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => this.assembleSet();

        protected string assembleSet()
        {
            if (this._values == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            if (this._values.Count > 1)
            {
                sb.Append("{");
            }

            this._values
                .Aggregate((a, b) => $"{a}, {b}")
                .To(sb.Append);

            if (this._values.Count > 1)
            {
                sb.Append("}");
            }
            
            return sb.ToString();
        }

        private string getAttributeValue(Attribute att) => att.GetType().GetCustomAttribute<TagAttribute>().Tag;

        private string getAttributeValue(Level level) => level.GetType().GetCustomAttribute<TagAttribute>().Tag;

        private string assembleExtension(string str) => $"{this.assembleSet()}.{str}";
    }
}

﻿using System;
using System.Collections.Generic;

namespace YoFi.Core.Reports;

/// <summary>
/// Dictionary of (<typeparamref name="TColumn"/>,<typeparamref name="TRow"/>)
/// to <typeparamref name="TValue"/>.
/// </summary>
/// <typeparam name="TColumn">Class to represent each column</typeparam>
/// <typeparam name="TRow">Class to represent each row</typeparam>
/// <typeparam name="TValue">Class to represent each cell value</typeparam>
public class Table<TColumn, TRow, TValue>
{
    /// <summary>
    /// Combined key of <typeparamref name="TColumn"/> and <typeparamref name="TRow"/>
    /// </summary>
    class Key
    {
        /// <summary>
        /// Column
        /// </summary>
        public TColumn column { get; }

        /// <summary>
        /// Row
        /// </summary>
        public TRow row { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="_column">Column</param>
        /// <param name="_row">Row</param>
        public Key(TColumn _column, TRow _row)
        {
            column = _column;
            row = _row;
        }

        public override bool Equals(object obj)
        {
            return obj is Key key &&
                   EqualityComparer<TColumn>.Default.Equals(column, key.column) &&
                   EqualityComparer<TRow>.Default.Equals(row, key.row);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(column, row);
        }
    }

    /// <summary>
    /// Primary representation of data.
    /// </summary>
    readonly Dictionary<Key,TValue> DataSet = new();

    /// <summary>
    /// Column labels
    /// </summary>
    public HashSet<TColumn> ColumnLabels { get; private set; } = new HashSet<TColumn>();

    /// <summary>
    /// Row labels
    /// </summary>
    public HashSet<TRow> RowLabels { get; set; } = new HashSet<TRow>();

    /// <summary>
    /// Value at this (C,R) position, or default
    /// </summary>
    /// <param name="columnlabel">Column label</param>
    /// <param name="rowlabel">Row label</param>
    /// <returns>Value at this (C,R) position, or default</returns>
    public TValue this[TColumn columnlabel, TRow rowlabel]
    {
        get
        {
            var key = new Key(_column: columnlabel, _row: rowlabel);

            return DataSet.GetValueOrDefault(key);
        }
        set
        {
            var key = new Key(_column: columnlabel, _row: rowlabel);

            DataSet[key] = value;
            ColumnLabels.Add(columnlabel);
            RowLabels.Add(rowlabel);
       }
    }
}

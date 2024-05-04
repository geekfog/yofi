﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace YoFi.Core.Models
{
    /// <summary>
    /// Budget Transaction (Budget line item)
    /// </summary>
    /// <remarks>
    /// Represents a single expected outlay of money into a specific account
    /// in a specific timeframe.
    /// </remarks>
    public class BudgetTx: IReportable, IModelItem<BudgetTx>, IImportDuplicateComparable
    {
        /// <summary>
        /// Object identity in Entity Framework
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The amount of expected outlay (typicaly, or income if positive)
        /// </summary>
        [DisplayFormat(DataFormatString = "{0:C2}")]
        [Column(TypeName = "decimal(18,2)")]
        [Editable(true)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Timeframe of expected outlay
        /// </summary>
        /// <remarks>
        /// Current practice is to have a single budget trasnaction in a year for
        /// year-long budget, and then multiple for budget that becomes available
        /// over time.
        /// </remarks>
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        [Editable(true)]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Category of expected outlay
        /// </summary>
        [Editable(true)]
        public string Category { get; set; }

        /// <summary>
        /// How many times is budget item tracked throughout the year
        /// </summary>
        /// <remarks>
        /// e.g. for a budget tracked monthly, use '12' here. The Amount is still yearly
        /// </remarks>
        public int Frequency { get; set; }

        [NotMapped]
        public string FrequencyName
        {
            get
            {
                if (Frequency < 0)
                    return "Invalid";
                else return Frequency switch
                {
                    0 => "Yearly",
                    1 => "Yearly",
                    4 => "Quarterly",
                    12 => "Monthly",
                    52 => "Weekly",
                    _ => Frequency.ToString()
                };
            }
            set
            {
                Frequency = value switch
                {
                    "Quarterly" => 4,
                    "Monthly" => 12,
                    "Weekly" => 52,
                    _ => 1,
                };
            }
        }

        public enum FrequencyEnum { Yearly = 1, Quarterly = 4, Monthly = 12, Weekly = 52 };

        /// <summary>
        /// Additional information about this budget line item for reference
        /// </summary>
        [Editable(true)]
        [Category("TestKey")]
        public string Memo { get; set; }

        /// <summary>
        /// Whether this object will be included in the next bulk operation
        /// </summary>
        public bool? Selected { get; set; }

        public class Reportable : IReportable
        {
            public decimal Amount { get; set; }

            public DateTime Timestamp { get; set; }

            public string Category { get; set; }
        }

        /// <summary>
        /// Divide this line item up into individual components through the year
        /// </summary>
        public IEnumerable<IReportable> Reportables
            => Enumerable
                .Range(0, Frequency)
                .Select(x => new Reportable()
                {
                    Timestamp = Period(x),
                    Category = Category,
                    Amount = Amount / Frequency
                });

        /// <summary>
        /// Timestamp for the nth instance of our divided period
        /// </summary>
        /// <param name="which"></param>
        private DateTime Period(int which)
        {
            if (Frequency <= 1 || Frequency > 365)
                return Timestamp;
            if (Frequency == 365)
                return Timestamp + TimeSpan.FromDays(which);
            if (12 % Frequency == 0)
                return new DateTime(Timestamp.Year, 1 + which * (12/Frequency), 1);
            return Timestamp + TimeSpan.FromDays((364 / Frequency) * which);
        }

        // TODO: This can be combined with ImportEquals. ImportEquals is actually a better equality comparer

        public override bool Equals(object obj)
        {
            return obj is BudgetTx tx &&
                   Timestamp == tx.Timestamp &&
                   Category == tx.Category;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Timestamp, Category);
        }

        public IQueryable<BudgetTx> InDefaultOrder(IQueryable<BudgetTx> original)
        {
            return original.OrderByDescending(x => x.Timestamp.Year).ThenByDescending(x => x.Timestamp.Month).ThenByDescending(x => x.Timestamp.Day).ThenBy(x => x.Category);
        }

        int IImportDuplicateComparable.GetImportHashCode() =>
            HashCode.Combine(Timestamp.Year, Timestamp.Month, Category);

        bool IImportDuplicateComparable.ImportEquals(object other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (other is not BudgetTx)
                throw new ArgumentException("Expected BudgetTx", nameof(other));

            var item = other as BudgetTx;

            return Timestamp.Year == item.Timestamp.Year && Timestamp.Month == item.Timestamp.Month && Category == item.Category;
        }
    }
}

using eSearch.Utils;
using Lucene.Net.Codecs;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSearch.Models.Search.LuceneCustomFieldComparers
{
    public class NumericStringComparerSource : FieldComparerSource
    {
        private readonly bool ascending;
        private readonly IFieldValueProcessor? processor;

        
        

        public NumericStringComparerSource(bool ascending, IFieldValueProcessor? fieldValueProcessor)
        {
            this.ascending = ascending;
            this.processor = fieldValueProcessor;
        }

        public override FieldComparer NewComparer(string fieldName, int numHits, int sortPos, bool reversed)
        {
            return new NumericStringComparer(fieldName, numHits, ascending, processor);
        }
    }

    public class NumericStringComparer : FieldComparer
    {
        private IFieldValueProcessor? fieldValueProcessor;
        private readonly string fieldName;
        private readonly int numHits;
        private readonly bool ascending;
        private object[] values;
        AtomicReaderContext context;
        private bool isString;
        private SortedDocValues  stringIndex;
        private NumericDocValues numericValues;

        private NaturalStringComparer naturalStringComparer;


        public NumericStringComparer(string fieldName, int numHits, bool ascending, IFieldValueProcessor? processor)
        {
            this.fieldName = fieldName;
            this.numHits = numHits;
            this.ascending = ascending;
            this.values = new object[numHits];
            this.fieldValueProcessor = processor;
            this.naturalStringComparer = new NaturalStringComparer();
        }

        public override int Compare(int slot1, int slot2)
        {
            object val1 = values[slot1];
            object val2 = values[slot2];

            if (val1 == null && val2 == null) return 0;
            if (val1 == null) return ascending ? -1 : 1;
            if (val2 == null) return ascending ? 1 : -1;

            if (val1 is string && val2 is string)
            {
                int result = naturalStringComparer.Compare((string)val1, (string)val2);
                return ascending ? result : -result;
            }
            if (val1 is long && val2 is long)
            {
                int result = ((long)val1).CompareTo((long)val2);
                return ascending ? result : -result;
            }
            throw new NotSupportedException("Unsupported or mismatched types");
        }


        public static bool IsNumeric(string input, out double num)
        {
            if (string.IsNullOrEmpty(input))
            {
                num = 0;
                return false;
            }

            int index = 0;
            int length = input.Length;

            // Handle optional negative sign
            if (index < length && input[index] == '-')
                index++;

            // Must have at least one digit
            if (index >= length)
            {
                num = 0;
                return false;
            }
                

            bool hasDigit = false;

            while (index < length)
            {
                char c = input[index];

                if (c >= '0' && c <= '9')
                {
                    hasDigit = true;
                }
                else
                {
                    num = 0;
                    return false;
                }

                index++;
            }
            if (hasDigit)
            {
                if (double.TryParse(input, out num))
                {
                    return true;
                }
            }

            num = 0;
            return false;
        }


        public override void SetBottom(int slot)
        {
            // Not needed for this example
        }

        

        public override int CompareBottom(int doc)
        {
            // Not needed for this example
            return 0;
        }

        public override void Copy(int slot, int doc)
        {
            if (isString)
            {
                int ord = stringIndex.GetOrd(doc);
                if (ord == -1)
                {
                    values[slot] = null; // No value for this document.
                }
                else
                {
                    BytesRef term = new BytesRef();
                    stringIndex.LookupOrd(ord, term);
                    string value = term.Utf8ToString();
                    if (fieldValueProcessor != null)
                    {
                        values[slot] = fieldValueProcessor.ProcessFieldValueForSorting(value);
                    } else
                    {
                        values[slot] = value.ToLowerInvariant();
                    }
                    
                }
            } else
            {
                values[slot] = numericValues.Get(doc);
            }
        }

        public override FieldComparer SetNextReader(AtomicReaderContext context)
        {
            this.context = context;
            FieldInfo fieldInfo = context.AtomicReader.FieldInfos.FieldInfo(fieldName);
            if (fieldInfo?.DocValuesType == DocValuesType.NUMERIC)
            {
                numericValues = context.AtomicReader.GetNumericDocValues(fieldName);
                isString = false;
            }
            else
            {
                // Assume string.
                stringIndex = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, fieldName);
                isString = true;
            }
            return this;
        }

        public override int CompareValues(object first, object second)
        {
            return 0;
        }

        public override void SetTopValue<TValue>(TValue value)
        {
            // Not needed
        }

        public override int CompareTop(int doc)
        {
            return 0;
        }

        

        public override object GetValue(int slot)
        {
            return values[slot];
        }
    }
}

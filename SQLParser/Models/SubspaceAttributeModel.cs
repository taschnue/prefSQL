using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace prefSQL.SQLParser.Models
{
    struct SubspaceAttributeModel
    {
        private readonly string _fullColumnName, _columnName;

        public SubspaceAttributeModel(string fullColumnName, string columnName)
        {
            _fullColumnName = fullColumnName;
            _columnName = columnName;
        }

        public string FullColumnName { get {return _fullColumnName;} }

        public string ColumnName { get {return _columnName;} }

        public override int GetHashCode()
        {
            return 31 * FullColumnName.GetHashCode() + 17 * ColumnName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (GetType() != obj.GetType())
            {
                return false;
            }

            return Equals((SubspaceAttributeModel)obj);
        }

        public bool Equals(SubspaceAttributeModel obj)
        {
            if (obj == null)
            {
                return false;
            }

            return FullColumnName == obj.FullColumnName && ColumnName == obj.ColumnName;
        }

        public static bool operator ==(SubspaceAttributeModel a, SubspaceAttributeModel b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.FullColumnName == b.FullColumnName && a.ColumnName == b.ColumnName;
        }

        public static bool operator !=(SubspaceAttributeModel a, SubspaceAttributeModel b)
        {
            return !(a == b);
        }
    }
}

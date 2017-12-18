using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Refazer.Web.Models
{
    public class Example
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public String Question { get; set; }

        [Required]
        public String IncorrectCode { get; set; }

        [Required]
        public String CorrectCode { get; set; }

        [Required]
        public String EndPoint { get; set; }

        [ForeignKey("EndPoint")]
        public Assignment Assignment { get; set; }

        public String KeyPoint()
        {
            return EndPoint + "/" + Question + "/";
        }

        public override bool Equals(Object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            return Id == ((Example)obj).Id;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = (hash * 23) + Id.GetHashCode();
            return hash;
        }
    }
}
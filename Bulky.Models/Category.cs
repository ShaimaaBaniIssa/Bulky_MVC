using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bulky.Models
{
    public class Category
    {

        // automatically treat this property as primary key
        [Key]
        public int Id { get; set; }
        [Required] //not null
        [MaxLength(30)] //for validation
        [DisplayName("Category Name")]
        public string Name { get; set; }
        [DisplayName("Display Order")]
        [Range(1, 100)]
        public int DisplayOrder { get; set; }
    }
}

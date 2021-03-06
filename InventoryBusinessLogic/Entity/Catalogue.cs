namespace InventoryBusinessLogic.Entity
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;
    using Newtonsoft.Json;

    [Table("Catalogue")]
    public partial class Catalogue
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Catalogue()
        {
            AdjustmentItem = new HashSet<AdjustmentItem>();
            PurchaseItem = new HashSet<PurchaseItem>();
            Request = new HashSet<Request>();
        }

        [Key]
        [StringLength(20)]
        public string ItemID { get; set; }

        [StringLength(50)]
        public string Description { get; set; }

        public int? Quantity { get; set; }

        [StringLength(50)]
        public string Category { get; set; }

        public int? ReorderLevel { get; set; }

        public int? ReorderQuantity { get; set; }

        [StringLength(50)]
        public string MeasureUnit { get; set; }

        public double? Price { get; set; }

        [StringLength(20)]
        public string Supplier1 { get; set; }

        [StringLength(20)]
        public string Supplier2 { get; set; }

        [StringLength(20)]
        public string Supplier3 { get; set; }

        [StringLength(20)]
        public string BinNumber { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [JsonIgnore]
        public virtual ICollection<AdjustmentItem> AdjustmentItem { get; set; }

        [JsonIgnore]
        public virtual Supplier Supplier { get; set; }
        [JsonIgnore]
        public virtual Supplier Supplier4 { get; set; }

        [JsonIgnore]
        public virtual Supplier Supplier5 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [JsonIgnore]
        public virtual ICollection<PurchaseItem> PurchaseItem { get; set; }
        [JsonIgnore]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

        public virtual ICollection<Request> Request { get; set; }
    }
}

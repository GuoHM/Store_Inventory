﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryBusinessLogic.Entity;

namespace InventoryBusinessLogic
{
    public class PurchaseItemBusinessLogic
    {
        Inventory inventory = new Inventory();

        public void addPurchaseItem(PurchaseItem purchaseItem)
        {
            inventory.PurchaseItem.Add(purchaseItem);
            inventory.SaveChanges();
        }

    }
}
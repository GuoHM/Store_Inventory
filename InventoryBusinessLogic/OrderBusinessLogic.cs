﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InventoryBusinessLogic.Entity;

namespace InventoryBusinessLogic
{
    public class OrderBusinessLogic
    {
        Inventory inventory = new Inventory();

        /// <summary>
        /// generate order id by current login user department and current week
        /// </summary>
        /// <param name="id">current login user id</param>
        /// <returns></returns>
        public string generateOrderIDById(string id)
        {
            string result="";
            string departmentID = inventory.AspNetUsers.Where(x => x.Id == id).First().DepartmentID;
            result += departmentID + GetWeekOfYear() + DateTime.Now.Year;
            return result;
        }

        private string GetWeekOfYear()
        {
            //1. find the last day in firstweek of this year
            int firstWeekend = 7 - Convert.ToInt32(DateTime.Parse(DateTime.Today.Year + "-1-1").DayOfWeek);

            //2. get today is which days of this year
            int currentDay = DateTime.Today.DayOfYear;

            //3. (today - the first weekend day)/7
            int code =  Convert.ToInt32(Math.Ceiling((currentDay - firstWeekend) / 7.0)) + 1;
            if (code < 10)
            {
                return "0" + code;
            }
            return ""+code;
        }

        public Order GetOrderByOrderId(string orderid)
        {
            try
            {
                return inventory.Order.Where(x => x.OrderID == orderid).First();
            } catch (Exception)
            {
                return null;
            }         
        }

        public void addOrder(Order order)
        {
            inventory.Order.Add(order);
            inventory.SaveChanges();
        }

        public void updateOrder(Order order)
        {
            Order update = inventory.Order.Where(x => x.OrderID == order.OrderID).First();
            update.OrderDate = order.OrderDate;
            update.OrderStatus = order.OrderStatus;
            update.TotalPrice = order.TotalPrice;
            update.Signature = order.Signature;
            update.DepartmentID = order.DepartmentID;
            inventory.SaveChanges();
        }


    }
}
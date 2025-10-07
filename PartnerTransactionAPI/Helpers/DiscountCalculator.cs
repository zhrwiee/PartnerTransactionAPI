using System;
using System.Collections.Generic;

namespace YourProjectNamespace.Helpers
{
    public static class DiscountCalculator
    {
        public static Dictionary<string, object> Calculate(decimal totalAmount)
        {
            decimal baseDiscount = 0;
            decimal extraDiscount = 0;

            // Base Discount Rule
            if (totalAmount < 200)
                baseDiscount = 0;
            else if (totalAmount <= 500)
                baseDiscount = 5;
            else if (totalAmount <= 800)
                baseDiscount = 7;
            else if (totalAmount <= 1200)
                baseDiscount = 10;
            else
                baseDiscount = 15;

            // Conditional Discounts
            if (totalAmount > 500 && IsPrime((int)totalAmount))
                extraDiscount += 8;

            if (totalAmount > 900 && totalAmount % 10 == 5)
                extraDiscount += 10;

            decimal totalDiscountPercent = baseDiscount + extraDiscount;

            // Cap maximum discount to 20%
            if (totalDiscountPercent > 20)
                totalDiscountPercent = 20;

            decimal totalDiscount = totalAmount * totalDiscountPercent / 100;
            decimal finalAmount = totalAmount - totalDiscount;

            return new Dictionary<string, object>
            {
                {"result", 1},
                {"totalamount", totalAmount},
                {"totaldiscount", totalDiscount},
                {"finalamount", finalAmount}
            };
        }

        private static bool IsPrime(int number)
        {
            if (number <= 1) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;

            for (int i = 3; i * i <= number; i += 2)
            {
                if (number % i == 0)
                    return false;
            }
            return true;
        }
    }
}

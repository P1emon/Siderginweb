namespace MyEStore.Helpers
{
    public static class CurrencyHelper
    {
        public static string FormatVND(decimal amount)
        {
            return string.Format("{0:N0} đ", amount);
        }
    }

}

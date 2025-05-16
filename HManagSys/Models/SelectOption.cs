namespace HManagSys.Models
{

    /// <summary>
    /// Option pour les listes déroulantes
    /// </summary>
    public class SelectOption
    {
        public string Value { get; set; }
        public string Text { get; set; }

        public SelectOption(string value, string text)
        {
            Value = value;
            Text = text;
        }
    }
}

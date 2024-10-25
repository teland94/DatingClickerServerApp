namespace DatingClickerServerApp.Components.Model
{
    public class SelectableItem<T>
    {
        public T Item { get; set; }

        public bool IsSelected { get; set; }
    }
}
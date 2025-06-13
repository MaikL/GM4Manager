using GM4ManagerWPF.Interfaces;

namespace GM4ManagerWPF.Classes
{
    public class CursorService: ICursorService
    {
        public void SetBusyCursor()
        {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
        }
        public void ResetCursor()
        {
            System.Windows.Input.Mouse.OverrideCursor = null;
        }
    }
}

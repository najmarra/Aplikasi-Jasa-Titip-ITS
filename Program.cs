namespace JastipITS;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            MessageBox.Show(ex.ToString(), "Jastip ITS - Error");
        }
    }
}
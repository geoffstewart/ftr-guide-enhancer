/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 28/09/2010
 * Time: 9:12 PM
 * 
 */

namespace GuideEnricher
{
    using System.ServiceProcess;

    public static class Program
    {
        /// <summary>
        /// This method starts the service.
        /// </summary>
        public static void Main()
        {
            // This should be the production code, starts the service...
            ServiceBase.Run(new ServiceBase[] { new GuideEnricher() });
            
            // Use the following when debuging from VS
            //new GuideEnricher().Start();
        }
    }
}

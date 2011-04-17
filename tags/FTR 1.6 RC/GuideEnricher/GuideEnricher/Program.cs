/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 28/09/2010
 * Time: 9:12 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Threading;

using ForTheRecord.ServiceAgents;
using ForTheRecord.ServiceContracts;
using ForTheRecord.Entities;
using ForTheRecord.Client.Common;

namespace GuideEnricher
{
   static class Program
   {
      
//      private static string MODULE="windowService";
      
      /// <summary>
      /// This method starts the service.
      /// </summary>
      static void Main()
      {
         // To run more than one service you have to add them here
         ServiceBase.Run(new ServiceBase[] { new GuideEnricher() });
      }
      

      
   }
}

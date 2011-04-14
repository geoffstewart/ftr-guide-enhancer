/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 06/10/2010
 * Time: 5:05 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using ForTheRecord.Entities;

namespace GuideEnricher
{
   /// <summary>
   /// Description of DataEnricher.
   /// </summary>
   public interface IDataEnricher
   {
      IProgramSummary enrichProgram(IProgramSummary existingProgram);
      void close();
      
   }
}

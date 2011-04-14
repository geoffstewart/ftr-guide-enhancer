/*
 * Created by SharpDevelop.
 * User: geoff
 * Date: 07/12/2010
 * Time: 1:17 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Runtime.Serialization;

namespace GuideEnricher
{
   /// <summary>
   /// Desctiption of DataEnricherException.
   /// </summary>
   public abstract class DataEnricherException : Exception, ISerializable
   {
      public DataEnricherException()
      {
      }

       public DataEnricherException(string message) : base(message)
      {
      }

      public DataEnricherException(string message, Exception innerException) : base(message, innerException)
      {
      }

      // This constructor is needed for serialization.
      protected DataEnricherException(SerializationInfo info, StreamingContext context) : base(info, context)
      {
      }
   }
}
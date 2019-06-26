using System;
using System.Globalization;

public class InputDataReader {
   public class InputDataReaderException : System.Exception {
        public InputDataReaderException(string file) : base("'" + file + "' contains bad data format") {
         
      }
   }

    public string[] _tokens;
    public int      _current;
    public string   _fileName;

    public NumberFormatInfo _nfi = NumberFormatInfo.InvariantInfo;

    public string NextToken() {
      string token = _tokens[_current++];
      while ( token == "" )
         token = _tokens[_current++];

      return token;
   }

   public InputDataReader(string fileName) {
      _fileName = fileName;
      System.IO.StreamReader reader = new System.IO.StreamReader(fileName);

      string text = reader.ReadToEnd();

      text = text.Replace("[", " [ ");
      text = text.Replace("]", " ] ");
      text = text.Replace(",", " , ");
      text = text.Replace('\"', ' ');

      _tokens = text.Split(null);

      reader.Close();
    
      _current = 0;
   }

    public double ReadDouble() {
      return Double.Parse(NextToken(), _nfi);
   }

    public int ReadInt() {
      return Int32.Parse(NextToken(), _nfi);
   }

    public string ReadString() {
      return NextToken();
   }

    public double[] ReadDoubleArray() {
      string token = NextToken(); // Read the '['
      
      if ( token != "[" )
         throw new InputDataReaderException(_fileName);
      
      System.Collections.ArrayList values = new System.Collections.ArrayList();
      token = NextToken();
      while (token != "]") {
         values.Add(Double.Parse(token, _nfi));
         token = NextToken();
         
         if ( token == "," ) {
            token = NextToken();
         }
         else if ( token != "]" ) {
            throw new InputDataReaderException(_fileName);
         }
      }
      
      if ( token != "]" )
         throw new InputDataReaderException(_fileName);
    
      // Fill the array.
      double[] res = new double[values.Count];
      for (int i = 0; i < values.Count; i++) {
         res[i] = (double)values[i];
      }
      
      return res;
   }

   public double[][] ReadDoubleArrayArray() {
      string token = NextToken(); // Read the '['
      
      if ( token != "[" )
         throw new InputDataReaderException(_fileName);
      
      System.Collections.ArrayList values = new System.Collections.ArrayList();
      token = NextToken();
      
      while (token == "[") {
         _current--;
         
         values.Add(ReadDoubleArray());
         
         token = NextToken();
         if      ( token == "," ) {
           token = NextToken();
         }
         else if ( token != "]" ) {
           throw new InputDataReaderException(_fileName);
         }
      }
    
      if ( token != "]" )
         throw new InputDataReaderException(_fileName);
    
      // Fill the array.
      double[][] res = new double[values.Count][];
      for (int i = 0; i < values.Count; i++) 
         res[i] = (double[])values[i];
      
      return res;
   }

    public int[] ReadIntArray() {
      string token = NextToken(); // Read the '['
      
      if ( token != "[" )
         throw new InputDataReaderException(_fileName);
      
      System.Collections.ArrayList values = new System.Collections.ArrayList();
      token = NextToken();
      while (token != "]") {
         values.Add(Int32.Parse(token, _nfi));
         token = NextToken();
         
         if      ( token == "," ) {
            token = NextToken();
         }
         else if ( token != "]" ) {
            throw new InputDataReaderException(_fileName);
         }
      }
      
      if ( token != "]" )
         throw new InputDataReaderException(_fileName);
    
      // Fill the array.
      int[] res = new int[values.Count];
      for (int i = 0; i < values.Count; i++)
         res[i] = (int)values[i];
      
      return res;
   }

    public int[][] ReadIntArrayArray() {
      string token = NextToken(); // Read the '['
      
      if ( token != "[" )
         throw new InputDataReaderException(_fileName);
      
      System.Collections.ArrayList values = new System.Collections.ArrayList();
      token = NextToken();
      
      while (token == "[") {
         _current--;
         
         values.Add(ReadIntArray());
         
         token = NextToken();
         if      ( token == "," ) {
            token = NextToken();
         }
         else if ( token != "]" ) {
            throw new InputDataReaderException(_fileName);
         }
      }
    
      if ( token != "]" )
         throw new InputDataReaderException(_fileName);
    
      // Fill the array.
      int[][] res = new int[values.Count][];
      for (int i = 0; i < values.Count; i++)
         res[i] = (int[])values[i];
      
      return res;
   }

    public string[] ReadStringArray() {
      string token = NextToken(); // Read the '['
      
      if ( token != "[" )
         throw new InputDataReaderException(_fileName);
      
      System.Collections.ArrayList values = new System.Collections.ArrayList();
      token = NextToken();
      while (token != "]") {
         values.Add(token);
         token = NextToken();
         
         if      ( token == "," ) {
            token = NextToken();
         }
         else if ( token != "]" ) {
            throw new InputDataReaderException(_fileName);
         }
      }
      
      if ( token != "]" )
         throw new InputDataReaderException(_fileName);
    
      // Fill the array.
      string[] res = new string[values.Count];
      for (int i = 0; i < values.Count; i++) 
         res[i] = (string)values[i];
      
      return res;
   }

    public string[][] ReadStringArrayArray() {
      string token = NextToken(); // Read the '['
      
      if ( token != "[" )
         throw new InputDataReaderException(_fileName);
      
      System.Collections.ArrayList values = new System.Collections.ArrayList();
      token = NextToken();
      
      while (token == "[") {
         _current--;
         
         values.Add(ReadStringArray());
         
         token = NextToken();
         if      ( token == "," ) {
            token = NextToken();
         }
         else if ( token != "]" ) {
            throw new InputDataReaderException(_fileName);
         }
      }
    
      if ( token != "]" )
         throw new InputDataReaderException(_fileName);
    
      // Fill the array.
      string[][] res = new string[values.Count][];
      for (int i = 0; i < values.Count; i++)
         res[i] = (string[])values[i];
      
      return res;
   }
}

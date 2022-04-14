/*    Arithmetic compression in C#                                                                                                                                                                   
    A simple, single-file C# arithmetic compressor/decompressor                                                                                                                                      
                                                                                                                                                                                                     
    Source version 0.5, April, 2009                                                                                                                                                                  
                                                                                                                                                                                                     
    Copyright (C) 2009 Chris Lomont                                                                                                                                                                  
                                                                                                                                                                                                     
    This software is provided 'as-is', without any express or implied                                                                                                                                
    warranty.  In no event will the author be held liable for any damages                                                                                                                            
    arising from the use of this software.                                                                                                                                                           
                                                                                                                                                                                                     
    Permission is granted to anyone to use this software for any purpose,                                                                                                                            
    including commercial applications, and to alter it and redistribute it                                                                                                                           
    freely, subject to the following restrictions:                                                                                                                                                   
                                                                                                                                                                                                     
    1. The origin of this software must not be misrepresented; you must not                                                                                                                          
     claim that you wrote the original software. If you use this software                                                                                                                            
     in a product, an acknowledgment in the product documentation would be                                                                                                                           
     appreciated but is not required.                                                                                                                                                                
    2. Altered source versions must be plainly marked as such, and must not be                                                                                                                       
     misrepresented as being the original software.                                                                                                                                                  
    3. This notice may not be removed or altered from any source distribution.                                                                                                                       
                                                                                                                                                                                                     
    Chris Lomont, contact me through www.lomont.org                                                                                                                                                  
                                                                                                                                                                                                     
    This legalese is patterned after the zlib compression library                                                                                                                                    
*/                                                                                                                                                                                                   

using System;                                                                                                                                                                                        
using System.Collections.Generic;                                                                                                                                                                    
using System.Diagnostics;                                                                                                                                                                            

/* Sample program:                                                                                                                                                                                   
    // 1. Create a compressor/decompressor                                                                                                                                                           
    Lomont.Compression.ArithmeticCompressor comp = new ArithmeticCompressor();                                                                                                                       
    // 2. Get some data to compress as a byte array                                                                                                                                                  
    byte [] data = new byte[1000000]; // compress 1000000 zeros                                                                                                                                      
    // 3. compress the data to another byte array                                                                                                                                                    
    byte [] packedData = comp.Compress(data);                                                                                                                                                        
    // 4. decompress the data as desired                                                                                                                                                             
    byte [] unpackedData = comp.Decompress(packedData);                                                                                                                                              
    // 5. view the data sizes                                                                                                                                                                        
    Console.WriteLine("Sizes {0}->{1}->{2}", data.Length, packedData.Length, unpackedData.Length);                                                                                                   
 */                                                                                                                                                                                                  


/* Pick a probability model, or implement new ones to test here */                                                                                                                                   
using EncodeModel = Lomont.Compression.FastModel;                                                                                                                                                    
using DecodeModel = Lomont.Compression.FastModel;                                                                                                                                                    
//using EncodeModel = Lomont.Compression.SimpleModel;                                                                                                                                                
//using DecodeModel = Lomont.Compression.SimpleModel;                                                                                                                                                



/* TODO                                                                                                                                                                                              
 * 1. Investigate outputting a byte at a time for speed                                                                                                                                              
 * 2. Try other models - using array based sorted frequency tree - see solomon compression book                                                                                                      
 * 3. Test 16 bit symbols as well as 8 bit ones for speed                                                                                                                                            
 * 4. need encoder and decoder to have a FLUSH_CONTEXT symbol used when an error is about to occur, such as when underflow is about to occur or compute length possible to encode before code breaks.
 * 5. consider rewriting with range stored as [L,L+R) where L is Lower, R is Range note then that we need R>= number of symbols read at all times.                                                   
 * 6. in model, if total count > some max value, then divide all by two?! - keeps range/total > 0                                                                                                    
 * 7. Check two ways to compute new range - speed/versus compression tradeoff                                                                                                                        
FASTER                                                                                                                                                                                               
                ulong step = range / model.Total;                                                                                                                                                    
                Debug.Assert(step > 0);                                                                                                                                                              
                high = low + step * right - 1; // -1 for open interval                                                                                                                               
                low = low + step * left;                                                                                                                                                             
SLOWER                                                                                                                                                                                               
                // slightly more accurate, slightly slower                                                                                                                                           
                high = low + range* right/model.Total - 1; // -1 for openinterval                                                                                                                    
                low = low + range* left/model.Total;                                                                                                                                                 
                                                                                                                                                                                                     
 * 8. Streaming version                                                                                                                                                                              
 * 9. Single step version to encode parts of data at a time                                                                                                                                          
 * 10. Make model external - use interface                                                                                                                                                           
 * 11. Investigate "Piecewise Integer Mapping for Arithmetic Coding",Stuiver & Moffat, http://www.cs.mu.oz.au/~alistair/abstracts/sm98:dcc.html                                                      
 * 12. Investigate "An Improved Data Structure for Cumulative Probability Tables", Moffat, http://www.cs.mu.oz.au/~alistair/abstracts/mof99:spe.html                                                 
 * */                                                                                                                                                                                                

namespace Lomont                                                                                                                                                                                     
{                                                                                                                                                                                                
	namespace Compression                                                                                                                                                                            
	{                                                                                                                                                                                            
		/// <summary>                                                                                                                                                                                
		/// Provide class to do simple bytewise arithmetic compression/decompression                                                                                                                 
		/// </summary>                                                                                                                                                                               
		public class _ArithmeticCompressor                                                                                                                                                            
		{                                                                                                                                                                                        
			#region Interface                                                                                                                                                                        
			/// <summary>                                                                                                                                                                            
			/// Construct the codec, initialize constants                                                                                                                                            
			/// </summary>                                                                                                                                                                           
			public _ArithmeticCompressor()                                                                                                                                                            
			{                                                                                                                                                                                    
				maxRange = 1UL << bitlen;                                                                                                                                                            
				half = maxRange >> 1;                                                                                                                                                                
				quarter = half >> 1;                                                                                                                                                                 
				quarter3 = 3 * quarter;                                                                                                                                                              
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Compress byte data into a new array                                                                                                                                                  
			/// </summary>                                                                                                                                                                           
			/// <param name="array">The byte data to compress</param>                                                                                                                                
			/// <returns>The compressed byte array</returns>                                                                                                                                         
			public byte[] Compress(IEnumerable<byte> data)                                                                                                                                           
			{                                                                                                                                                                                    
				ResetEncoder(new EncodeModel(256));                                                                                                                                                  
				foreach (byte b in data)                                                                                                                                                             
					EncodeSymbol(b);                                                                                                                                                                 
				EncodeSymbol(model.EOF);                                                                                                                                                             
				FlushEncoder();                                                                                                                                                                      
				return writer.Data;                                                                                                                                                                  
			} // Compress                                                                                                                                                                        

			/// <summary>                                                                                                                                                                            
			/// Decompress data and return a byte array                                                                                                                                              
			/// </summary>                                                                                                                                                                           
			/// <param name="array">The byte data to decompress</param>                                                                                                                              
			/// <returns>The decompressed byte data</returns>                                                                                                                                        
			public byte[] Decompress(IEnumerable<byte> data)                                                                                                                                         
			{                                                                                                                                                                                    
				ResetDecoder(data, new DecodeModel(256));                                                                                                                                            
				List<byte> output = new List<byte>();                                                                                                                                                
				ulong symbol=0;                                                                                                                                                                      
				while ((symbol = DecodeSymbol()) != model.EOF)                                                                                                                                       
					output.Add((byte)symbol);                                                                                                                                                        
				return output.ToArray();                                                                                                                                                             
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Get optimal length in bits if perfect coding used.                                                                                                                                   
			/// Used to compare this implementation with perfection.                                                                                                                                 
			/// </summary>                                                                                                                                                                           
			public ulong OptimalLength                                                                                                                                                               
			{                                                                                                                                                                                    
				get                                                                                                                                                                                  
				{ // obtain this from the model                                                                                                                                                  
					return model.OptimalLength;                                                                                                                                                      
				}                                                                                                                                                                                
			}                                                                                                                                                                                    
			#endregion                                                                                                                                                                               


			#region Implementation                                                                                                                                                                   

			static int bitlen = 62;  // number of bits used - todo - analyze this and make optimal?                                                                                                  
			readonly ulong maxRange; // highest bit used, range is [0,1] = [0,maxRange]                                                                                                              
			readonly ulong half;     // half of the range [0,1)                                                                                                                                      
			readonly ulong quarter;  //  1/4 of the range [0,1)                                                                                                                                      
			readonly ulong quarter3; //  3/4 of the range [0,1)                                                                                                                                      

			/// <summary>                                                                                                                                                                            
			/// The compression model                                                                                                                                                                
			/// </summary>                                                                                                                                                                           
			IModel model;                                                                                                                                                                            

			/// <summary>                                                                                                                                                                            
			/// This member allows writing a bit at a time                                                                                                                                           
			/// </summary>                                                                                                                                                                           
			BitWriter writer;                                                                                                                                                                        
			/// <summary>                                                                                                                                                                            
			/// This member allows reading a bit at a time                                                                                                                                           
			/// </summary>                                                                                                                                                                           
			BitReader reader;                                                                                                                                                                        

			// leave highest bits open to prevent overflow                                                                                                                                           
			ulong rangeHigh; // the high value of the current range [rangeLow,rangeHigh)                                                                                                             
			ulong rangeLow;  // the low value of the current range [rangeLow,rangeHigh)                                                                                                              
			long underflow;  // track how many underflows are unaccounted for                                                                                                                        

			/// <summary>                                                                                                                                                                            
			/// Encode a single symbol, updating internals                                                                                                                                           
			/// </summary>                                                                                                                                                                           
			/// <param name="symbol">Symbol to encode</param>                                                                                                                                        
			void EncodeSymbol(ulong symbol)                                                                                                                                                          
			{                                                                                                                                                                                    
				#if DEBUG                                                                                                                                                                                            
				checked                                                                                                                                                                              
				#endif                                                                                                                                                                                               
				{                                                                                                                                                                                
					Debug.Assert(rangeLow < rangeHigh);                                                                                                                                              
					ulong range = rangeHigh - rangeLow + 1, left, right; // +1 for open interval                                                                                                     
					model.GetRangeFromSymbol(symbol, out left, out right);                                                                                                                           

					ulong step = range / model.Total;                                                                                                                                                
					Debug.Assert(step > 0);                                                                                                                                                          
					rangeHigh = rangeLow + step * right - 1; // -1 for open interval                                                                                                                 
					rangeLow = rangeLow + step * left;                                                                                                                                               

					model.AddSymbol(symbol); // this has to be done AFTER range lookup so decoder can follow it                                                                                      

					// todo - analyze loops: see if needs to be 2 in 1, and see if E3 loop needs merged                                                                                              
					// scaling types E1, E2, E3                                                                                                                                                      
					while ((rangeHigh < half) || (half <= rangeLow))                                                                                                                                 
					{                                                                                                                                                                            
						if (rangeHigh < half)                                                                                                                                                        
						{ // E1 type scaling                                                                                                                                                     
							writer.Write(0);                                                                                                                                                         
							while (underflow > 0) { --underflow; writer.Write(1); }                                                                                                                  
							rangeHigh = (rangeHigh << 1) + 1;                                                                                                                                        
							rangeLow <<= 1;                                                                                                                                                          
						}                                                                                                                                                                        
						else                                                                                                                                                                         
						{ // E2 type scaling                                                                                                                                                     
							writer.Write(1);                                                                                                                                                         
							while (underflow > 0) { --underflow; writer.Write(0); }                                                                                                                  
							rangeHigh = ((rangeHigh - half) << 1) + 1;                                                                                                                               
							rangeLow = (rangeLow - half) << 1;                                                                                                                                       
						}                                                                                                                                                                        
					}                                                                                                                                                                            
					while ((quarter <= rangeLow) && (rangeHigh < quarter3))                                                                                                                          
					{ // E3 type scaling                                                                                                                                                         
						underflow++;   // todo - if about to overflow, need a flush context symbol?!                                                                                                 
						rangeLow = (rangeLow - quarter) << 1;                                                                                                                                        
						rangeHigh = ((rangeHigh - quarter) << 1) + 1;                                                                                                                                
					}                                                                                                                                                                            
					// todo - is high - low > half here? if so, assert it                                                                                                                            
					Debug.Assert(rangeHigh - rangeLow >= quarter);                                                                                                                                   
				}                                                                                                                                                                                
			} // EncodeSymbol                                                                                                                                                                    


			/// <summary>                                                                                                                                                                            
			/// Flush final data out for encoding.                                                                                                                                                   
			/// </summary>                                                                                                                                                                           
			void FlushEncoder()                                                                                                                                                                      
			{                                                                                                                                                                                    
				// write enough bits to finalize the location of the interval                                                                                                                        
				// interval always holds at least 1/4 of the range, so cases:                                                                                                                        
				if (rangeLow < quarter) // low < quarter < half <= high                                                                                                                              
				{                                                                                                                                                                                
					writer.Write(0); // low end of the range                                                                                                                                         
					for (int i = 0; i < underflow + 1; ++i) // need a 1 and then overflow bits                                                                                                       
						writer.Write(1);                                                                                                                                                             
				}                                                                                                                                                                                
				else // low < half < quarter3 <= high                                                                                                                                                
				{                                                                                                                                                                                
					writer.Write(1); // low end of range, decoder adds 0s automatically on decode                                                                                                    
				}                                                                                                                                                                                
				writer.Flush();                                                                                                                                                                      
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Call this to start encoding                                                                                                                                                          
			/// </summary>                                                                                                                                                                           
			/// <param name="model">Input of a probablity model</param>                                                                                                                              
			void ResetEncoder(IModel model)                                                                                                                                                          
			{                                                                                                                                                                                    
				this.model = model;                                                                                                                                                                  

				// leave highest bits open to prevent overflow                                                                                                                                       
				rangeHigh = half + half - 1;                                                                                                                                                         
				rangeLow = 0;                                                                                                                                                                        
				underflow = 0;                                                                                                                                                                       
				// prepare to output bits                                                                                                                                                            
				writer = new BitWriter();                                                                                                                                                            
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Call this to start decoding                                                                                                                                                          
			/// </summary>                                                                                                                                                                           
			/// <param name="data"></param>                                                                                                                                                          
			void ResetDecoder(IEnumerable<byte> data, IModel model)                                                                                                                                  
			{                                                                                                                                                                                    
				this.model = model;                                                                                                                                                                  

				// leave highest bits open to prevent overflow                                                                                                                                       
				rangeHigh = half + half - 1;                                                                                                                                                         
				rangeLow = 0;                                                                                                                                                                        
				underflow = 0;                                                                                                                                                                       

				currentDecodeValue = 0;                                                                                                                                                              
				reader = new BitReader(data);                                                                                                                                                        

				// get initial value in [0,1) scaled range                                                                                                                                           
				for (int i = 0; i < bitlen; ++i)                                                                                                                                                     
					currentDecodeValue = (currentDecodeValue << 1) | reader.Read();                                                                                                                  
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Decode the next symbol and returns it.                                                                                                                                               
			/// Once returns EOF, do not call this anymore                                                                                                                                           
			/// Also updates current value in range [0,1).                                                                                                                                           
			/// </summary>                                                                                                                                                                           
			/// <returns>The decoded symbol.</returns>                                                                                                                                               
			ulong DecodeSymbol()                                                                                                                                                                     
			{                                                                                                                                                                                    
				#if DEBUG                                                                                                                                                                                            
				checked                                                                                                                                                                              
				#endif                                                                                                                                                                                               
				{                                                                                                                                                                                
					ulong symbol = 0;                                                                                                                                                                
					Debug.Assert(rangeLow < rangeHigh);                                                                                                                                              
					ulong range = rangeHigh - rangeLow + 1, left, right;                                                                                                                             

					ulong step = range / model.Total;                                                                                                                                                
					Debug.Assert(step > 0);                                                                                                                                                          
					Debug.Assert(currentDecodeValue >= rangeLow);                                                                                                                                    
					Debug.Assert(rangeHigh >= currentDecodeValue);                                                                                                                                   
					ulong value = (currentDecodeValue - rangeLow) / step; // the interval location to lookup                                                                                         
					symbol = model.GetSymbolAndRange(value, out left, out right);                                                                                                                    
					rangeHigh = rangeLow + step * right - 1; // -1 for open interval                                                                                                                 
					rangeLow = rangeLow + step * left;                                                                                                                                               

					model.AddSymbol(symbol);                                                                                                                                                         

					// scaling types E1, E2, E3                                                                                                                                                      
					while ((rangeHigh < half) || (half <= rangeLow))                                                                                                                                 
					{                                                                                                                                                                            
						if (rangeHigh < half)                                                                                                                                                        
						{ // E1 type scaling                                                                                                                                                     
							rangeHigh = (rangeHigh << 1) + 1;                                                                                                                                        
							rangeLow <<= 1;                                                                                                                                                          
							currentDecodeValue = (currentDecodeValue << 1) | reader.Read();                                                                                                          
						}                                                                                                                                                                        
						else                                                                                                                                                                         
						{ // E2 type scaling                                                                                                                                                     
							rangeHigh = ((rangeHigh - half) << 1) + 1;                                                                                                                               
							rangeLow = (rangeLow - half) << 1;                                                                                                                                       
							currentDecodeValue = ((currentDecodeValue - half) << 1) | reader.Read();                                                                                                 
						}                                                                                                                                                                        
					}                                                                                                                                                                            
					while ((quarter <= rangeLow) && (rangeHigh < quarter3))                                                                                                                          
					{ // E3 type scaling                                                                                                                                                         
						rangeLow = (rangeLow - quarter) << 1;                                                                                                                                        
						rangeHigh = ((rangeHigh - quarter) << 1) + 1;                                                                                                                                
						currentDecodeValue = ((currentDecodeValue - quarter) << 1) | reader.Read();                                                                                                  
					}                                                                                                                                                                            
					return symbol;      // todo - can do this earlier to avoid final looping?                                                                                                        
				}                                                                                                                                                                                
			} // DecodeSymbol                                                                                                                                                                    


			/// <summary>                                                                                                                                                                            
			/// The current value of the the decoding state                                                                                                                                          
			/// This is in [rangeLow, rangeHigh]                                                                                                                                                     
			/// </summary>                                                                                                                                                                           
			ulong currentDecodeValue;                                                                                                                                                                

			#endregion // Implementation                                                                                                                                                             
		} // ArithmeticCompressor class                                                                                                                                                          

		#region Models                                                                                                                                                                               
		public interface IModel                                                                                                                                                                      
		{                                                                                                                                                                                        
			/// <summary>                                                                                                                                                                            
			/// This symbol marks the end of file.                                                                                                                                                   
			/// </summary>                                                                                                                                                                           
			ulong EOF {get; }                                                                                                                                                                        

			/// <summary>                                                                                                                                                                            
			/// The total number of symbols seen                                                                                                                                                     
			/// </summary>                                                                                                                                                                           
			ulong Total { get; set; }                                                                                                                                                                

			/// <summary>                                                                                                                                                                            
			/// Add a new symbol to the probability table                                                                                                                                            
			/// </summary>                                                                                                                                                                           
			/// <param name="symbol">The symbol to add</param>                                                                                                                                       
			void AddSymbol(ulong symbol);                                                                                                                                                            

			/// <summary>                                                                                                                                                                            
			/// Given a symbol, return the range it falls into                                                                                                                                       
			/// </summary>                                                                                                                                                                           
			/// <param name="symbol">The symbol to lookup</param>                                                                                                                                    
			/// <param name="low">The low end of the range</param>                                                                                                                                   
			/// <param name="high">The high end of the range</param>                                                                                                                                 
			void GetRangeFromSymbol(ulong symbol, out ulong low, out ulong high);                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Look up the symbol with given value, and return range                                                                                                                                
			/// </summary>                                                                                                                                                                           
			/// <param name="value"></param>                                                                                                                                                         
			/// <param name="low">The low end of the range</param>                                                                                                                                   
			/// <param name="high">The high end of the range</param>                                                                                                                                 
			/// <returns>The symbol</returns>                                                                                                                                                        
			ulong GetSymbolAndRange(ulong value, out ulong low, out ulong high);                                                                                                                     

			/// <summary>                                                                                                                                                                            
			/// Based on current counts, find optimal length of bits that the data could fit into                                                                                                    
			/// </summary>                                                                                                                                                                           
			ulong OptimalLength {get; }                                                                                                                                                              

		} // IModel                                                                                                                                                                              

		/// <summary>                                                                                                                                                                                
		/// This model represents a simple and straightforward modeling of                                                                                                                           
		/// data probabilities.                                                                                                                                                                      
		/// Uses a simple, straightforward model that can be used to                                                                                                                                 
		/// benchmark other models.                                                                                                                                                                  
		/// </summary>                                                                                                                                                                               
		class SimpleModel : IModel                                                                                                                                                                   
		{                                                                                                                                                                                        
			#region Interface                                                                                                                                                                        

			/// <summary>                                                                                                                                                                            
			/// End of File marker.                                                                                                                                                                  
			/// </summary>                                                                                                                                                                           
			public ulong EOF { get { return eof; } }                                                                                                                                                 

			/// <summary>                                                                                                                                                                            
			/// The total number of symbols seen                                                                                                                                                     
			/// </summary>                                                                                                                                                                           
			public ulong Total { get; set; }                                                                                                                                                         

			/// <summary>                                                                                                                                                                            
			/// Construct a new model with the requested number of symbols                                                                                                                           
			/// </summary>                                                                                                                                                                           
			/// <param name="size">The number of symbols needed to be stored.</param>                                                                                                                
			public SimpleModel(ulong size)                                                                                                                                                           
			{                                                                                                                                                                                    
				eof = size;                                                                                                                                                                          
				cumulativeCount = new ulong[EOF + 2];                                                                                                                                                
				// initialize counts to make distinct                                                                                                                                                
				for (ulong i = 0; i < (ulong)(cumulativeCount.Length-1); ++i)                                                                                                                        
					AddSymbol(i);                                                                                                                                                                    
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Add a new symbol to the probability table                                                                                                                                            
			/// </summary>                                                                                                                                                                           
			/// <param name="symbol">The symbol to add</param>                                                                                                                                       
			public void AddSymbol(ulong symbol)                                                                                                                                                      
			{                                                                                                                                                                                    
				for (ulong i = symbol + 1; i < (ulong)cumulativeCount.Length; ++i)                                                                                                                   
					cumulativeCount[i]++;                                                                                                                                                            
				++Total;                                                                                                                                                                             
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Given a symbol, return the range it falls into                                                                                                                                       
			/// </summary>                                                                                                                                                                           
			/// <param name="symbol">The symbol whose range to find</param>                                                                                                                          
			/// <param name="low">The low end of the range</param>                                                                                                                                   
			/// <param name="high">The high end of the range</param>                                                                                                                                 
			public void GetRangeFromSymbol(ulong symbol, out ulong low, out ulong high)                                                                                                              
			{                                                                                                                                                                                    
				ulong index = symbol;                                                                                                                                                                
				low = cumulativeCount[index];                                                                                                                                                        
				high = cumulativeCount[index + 1];                                                                                                                                                   
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Look up the symbol with given value, and return range                                                                                                                                
			/// </summary>                                                                                                                                                                           
			/// <param name="value">The value to find</param>                                                                                                                                        
			/// <param name="left">The left end of the found range</param>                                                                                                                           
			/// <param name="right">The right end of the range</param>                                                                                                                               
			/// <returns>The symbol whose range includes value</returns>                                                                                                                             
			public ulong GetSymbolAndRange(ulong value, out ulong left, out ulong right)                                                                                                             
			{                                                                                                                                                                                    
				for (ulong i = 0; i < (ulong)cumulativeCount.Length - 1; ++i)                                                                                                                        
				{                                                                                                                                                                                
					if ((cumulativeCount[i] <= value) && (value < cumulativeCount[i + 1]))                                                                                                           
					{                                                                                                                                                                            
						GetRangeFromSymbol(i, out left, out right);                                                                                                                                  
						return i;                                                                                                                                                                    
					}                                                                                                                                                                            
				}                                                                                                                                                                                
				// if this is reached there is an unknown error elsewhere in the process                                                                                                             
				Debug.Assert(false, "Illegal lookup overflow!");                                                                                                                                     
				left = right = UInt64.MaxValue;                                                                                                                                                      
				return UInt64.MaxValue;                                                                                                                                                              
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// based on current counts, find optimal length of bits that the data could fit into                                                                                                    
			/// </summary>                                                                                                                                                                           
			public ulong OptimalLength                                                                                                                                                               
			{                                                                                                                                                                                    
				get                                                                                                                                                                                  
				{                                                                                                                                                                                
					double sum = 0;                                                                                                                                                                  
					double total = (Total - (ulong)cumulativeCount.Length); // number of these symbols                                                                                               
					for (int i = 0; i < cumulativeCount.Length - 1; ++i)                                                                                                                             
					{                                                                                                                                                                            
						double freq = cumulativeCount[i + 1] - cumulativeCount[i] - 1;                                                                                                               
						if (freq > 0)                                                                                                                                                                
						{                                                                                                                                                                        
							double p = freq / total;                                                                                                                                                 
							sum += -Math.Log(p, 2) * freq;                                                                                                                                           
						}                                                                                                                                                                        
					}                                                                                                                                                                            
					return (ulong)(sum);                                                                                                                                                             
				}                                                                                                                                                                                
			}                                                                                                                                                                                    
			#endregion                                                                                                                                                                               

			#region Implementation                                                                                                                                                                   
			/// <summary>                                                                                                                                                                            
			/// cumulative counts for each symbol                                                                                                                                                    
			/// entry j is the count of all symbol frequencies up to (but not including) symbol j                                                                                                    
			/// </summary>                                                                                                                                                                           
			ulong[] cumulativeCount;                                                                                                                                                                 

			/// <summary>                                                                                                                                                                            
			/// store the eof value                                                                                                                                                                  
			/// </summary>                                                                                                                                                                           
			ulong eof;                                                                                                                                                                               
			#endregion                                                                                                                                                                               

		} // SimpleModel                                                                                                                                                                         

		/// <summary>                                                                                                                                                                                
		/// This model represents modeling of data probabilities                                                                                                                                     
		/// using a treebased structure to provide fast O(log n) operations                                                                                                                          
		/// </summary>                                                                                                                                                                               
		class FastModel : IModel                                                                                                                                                                     
		{                                                                                                                                                                                        
			#region Implementation                                                                                                                                                                   

			/// <summary>                                                                                                                                                                            
			/// End of File marker.                                                                                                                                                                  
			/// </summary>                                                                                                                                                                           
			public ulong EOF { get { return eof; } }                                                                                                                                                 

			/// <summary>                                                                                                                                                                            
			/// The total number of symbols seen                                                                                                                                                     
			/// </summary>                                                                                                                                                                           
			public ulong Total { get; set; }                                                                                                                                                         

			/// <summary>                                                                                                                                                                            
			/// Construct a new model with the requested number of symbols                                                                                                                           
			/// </summary>                                                                                                                                                                           
			/// <param name="size"></param>                                                                                                                                                          
			public FastModel(ulong size)                                                                                                                                                             
			{                                                                                                                                                                                    
				eof = size;                                                                                                                                                                          
				tree = new ulong[EOF + 2]; // todo - this causes a slight increase over EOF+1 symbols - rethink?                                                                                     
				// initialize counts to make distinct                                                                                                                                                
				for (int i = 0; i < tree.Length-1; ++i)                                                                                                                                              
					AddSymbol((ulong)i);                                                                                                                                                             
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Add a new symbol to the probability table                                                                                                                                            
			/// </summary>                                                                                                                                                                           
			/// <param name="symbol">The symbol to add to the table</param>                                                                                                                          
			public void AddSymbol(ulong symbol)                                                                                                                                                      
			{                                                                                                                                                                                    
				long k = (long)symbol;                                                                                                                                                               
				for (/**/ ; k < tree.Length; k |= k+1)                                                                                                                                               
					tree[k]++; // can add d here to increment element by d total instead of 1                                                                                                        
				++Total;                                                                                                                                                                             
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Given a symbol, return the range it falls into                                                                                                                                       
			/// </summary>                                                                                                                                                                           
			/// <param name="symbol">The symbol to add</param>                                                                                                                                       
			/// <param name="low">The low end of the range</param>                                                                                                                                   
			/// <param name="high">The high end of the range</param>                                                                                                                                 
			public void GetRangeFromSymbol(ulong symbol, out ulong low, out ulong high)                                                                                                              
			{ // this uses interesting property of this tree:                                                                                                                                    
				// the parent of the higher index node of two consecutive entries will                                                                                                             
				// appear as an ancestor of the lower index node. This allows computing                                                                                                            
				// the difference at that shared parent to get the range, then walking                                                                                                             
				// back on the lower index to get the lower bound.                                                                                                                                 

				ulong diff = tree[symbol];                                                                                                                                                           
				long b = (long)(symbol);                                                                                                                                                             
				long target = (b & (b + 1)) - 1; // when hit this index, subtract from diff                                                                                                          

				ulong sum = 0;                                                                                                                                                                       
				b = (long)(symbol - 1);                                                                                                                                                              
				for (; b >= 0; b = (b & (b + 1)) - 1)                                                                                                                                                
				{                                                                                                                                                                                
					if (b == target) diff -= sum;                                                                                                                                                    
					sum += tree[b];                                                                                                                                                                  
				}                                                                                                                                                                                
				if (b == target) diff -= sum;                                                                                                                                                        
				low = sum;                                                                                                                                                                           
				high = sum + diff;                                                                                                                                                                   
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Look up the symbol with given value, and return range                                                                                                                                
			/// </summary>                                                                                                                                                                           
			/// <param name="value">The value whose range is to be found</param>                                                                                                                     
			/// <param name="low">The low end of the range</param>                                                                                                                                   
			/// <param name="high">The high end of the range</param>                                                                                                                                 
			/// <returns>The symbol for this range</returns>                                                                                                                                         
			public ulong GetSymbolAndRange(ulong value, out ulong low, out ulong high)                                                                                                               
			{                                                                                                                                                                                    
				// binary search - todo - redo using bit properties of index entries                                                                                                                 
				// this takes O(log^2 n) but should take O(log n) with better searching                                                                                                              
				ulong N = (ulong)tree.Length;                                                                                                                                                        
				ulong bottom = 0;                                                                                                                                                                    
				ulong top = N;                                                                                                                                                                       
				while (bottom < top)                                                                                                                                                                 
				{                                                                                                                                                                                
					ulong mid = bottom + ((top - bottom) / 2);  // Note: not (low + high) / 2 !!                                                                                                     
					if (Query(0, (long)mid) <= value)                                                                                                                                                
						bottom = mid + 1;                                                                                                                                                            
					else                                                                                                                                                                             
						top = mid;                                                                                                                                                                   
				}                                                                                                                                                                                
				if (bottom < N)                                                                                                                                                                      
				{ // we found a value                                                                                                                                                            
					GetRangeFromSymbol(bottom, out low, out high);                                                                                                                                   
					Debug.Assert((low<=value) && (value < high));                                                                                                                                    
					return bottom;                                                                                                                                                                   
				}                                                                                                                                                                                
				// if this is reached there is an unknown error elsewhere in the process                                                                                                             
				Debug.Assert(false, "Illegal lookup overflow!");                                                                                                                                     
				low = high = UInt64.MaxValue;                                                                                                                                                        
				return UInt64.MaxValue;                                                                                                                                                              
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Find the optimal number of bits the data so far would fit into.                                                                                                                      
			/// </summary>                                                                                                                                                                           
			public ulong OptimalLength                                                                                                                                                               
			{                                                                                                                                                                                    
				get                                                                                                                                                                                  
				{                                                                                                                                                                                
					double sum = 0;                                                                                                                                                                  
					double total = (Total - (ulong)tree.Length+1); // number of these symbols                                                                                                        
					for (int i = 0; i < tree.Length-1; ++i)                                                                                                                                          
					{                                                                                                                                                                            
						double freq = Query(i,i)-1;                                                                                                                                                  
						if (freq > 0)                                                                                                                                                                
						{                                                                                                                                                                        
							double p = freq / total;                                                                                                                                                 
							sum += -Math.Log(p, 2) * freq;                                                                                                                                           
						}                                                                                                                                                                        
					}                                                                                                                                                                            
					return (ulong)(sum);                                                                                                                                                             
				}                                                                                                                                                                                
			}                                                                                                                                                                                    
			#endregion                                                                                                                                                                               


			#region Implementation                                                                                                                                                                   

			// Fenwick tree code based on that at http://www.algorithmist.com/index.php/Fenwick_tree                                                                                                 
			// In this implementation, the tree is represented by an array of ulongs                                                                                                                 
			// Elements are numbered by 0, 1, ..., n-1.                                                                                                                                              
			// tree[i] is sum of elements with indexes i&(i+1),i&(i+2),...,i, inclusive.                                                                                                             
			// (this is different from what is proposed in Fenwick's article.)                                                                                                                       

			// todo - add scaling of tree values by half when overflow close                                                                                                                         
			// this can be done by walking tree and dividing node by half? need to prevent any freq from becoming 0                                                                                  

			// Returns sum of elements with indices a..b, inclusive                                                                                                                                  
			ulong Query(long a, long b)                                                                                                                                                              
			{                                                                                                                                                                                    
				if (a == 0)                                                                                                                                                                          
				{                                                                                                                                                                                
					ulong sum = 0;                                                                                                                                                                   
					for (; b >= 0; b = (b & (b + 1)) - 1)                                                                                                                                            
						sum += tree[b];                                                                                                                                                              
					return sum;                                                                                                                                                                      
				}                                                                                                                                                                                
				else                                                                                                                                                                                 
				{                                                                                                                                                                                
					return Query(0, b) - Query(0, a - 1);                                                                                                                                            
				}                                                                                                                                                                                
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Cumulative counts for each symbol stored in Fenwick tree.                                                                                                                            
			/// Entry i contains sum of elements i&(i+1), i&(i+2),...,i                                                                                                                              
			/// See above for more details on how stored.                                                                                                                                            
			/// </summary>                                                                                                                                                                           
			ulong[] tree;                                                                                                                                                                            

			/// <summary>                                                                                                                                                                            
			/// The EOF symbol                                                                                                                                                                       
			/// </summary>                                                                                                                                                                           
			ulong eof;                                                                                                                                                                               
			#endregion                                                                                                                                                                               
		} // FastModel                                                                                                                                                                           
		#endregion                                                                                                                                                                                   

		#region BitIO                                                                                                                                                                                

		class BitWriter                                                                                                                                                                              
		{                                                                                                                                                                                        
			#region Interface                                                                                                                                                                        
			/// <summary>                                                                                                                                                                            
			/// Create a class that outputs bits one at a time, MSB first                                                                                                                            
			/// </summary>                                                                                                                                                                           
			public BitWriter()                                                                                                                                                                       
			{                                                                                                                                                                                    
				bitPos = 0;                                                                                                                                                                          
				encodeData = new List<byte>();                                                                                                                                                       
				datum = 0;                                                                                                                                                                           
			}                                                                                                                                                                                    
			/// <summary>                                                                                                                                                                            
			/// Output a single symbol                                                                                                                                                               
			/// </summary>                                                                                                                                                                           
			/// <param name="bit"></param>                                                                                                                                                           
			public void Write(byte bit)                                                                                                                                                              
			{                                                                                                                                                                                    
				datum <<= 1;                                                                                                                                                                         
				datum = (byte)(datum | (bit & 1));                                                                                                                                                   
				++bitPos;                                                                                                                                                                            
				if (bitPos == 8)                                                                                                                                                                     
				{ // NOTE: no need to zero datum - it gets pushed along                                                                                                                          
					encodeData.Add(datum);                                                                                                                                                           
					bitPos = 0;                                                                                                                                                                      
				}                                                                                                                                                                                
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Fill in final byte with 0s                                                                                                                                                           
			/// Call before obtaining Data                                                                                                                                                           
			/// </summary>                                                                                                                                                                           
			public void Flush()                                                                                                                                                                      
			{                                                                                                                                                                                    
				while (bitPos != 0)                                                                                                                                                                  
					Write(0);                                                                                                                                                                        
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Obtain the internal data as a byte array                                                                                                                                             
			/// </summary>                                                                                                                                                                           
			public byte [] Data { get { return encodeData.ToArray(); } }                                                                                                                             
			#endregion                                                                                                                                                                               

			#region Implementation                                                                                                                                                                   
			int bitPos = 0;        // bits used in current byte                                                                                                                                      
			byte datum;               // current byte being created                                                                                                                                  
			List<byte> encodeData; // data created                                                                                                                                                   
			#endregion                                                                                                                                                                               
		} // BitWriter                                                                                                                                                                           

		class BitReader                                                                                                                                                                              
		{                                                                                                                                                                                        
			#region Interface                                                                                                                                                                        
			/// <summary>                                                                                                                                                                            
			/// Construct a reader to parse one bit at a time, MSB first                                                                                                                             
			/// </summary>                                                                                                                                                                           
			/// <param name="data">The data to read from</param>                                                                                                                                     
			public BitReader(IEnumerable<byte> data)                                                                                                                                                 
			{                                                                                                                                                                                    
				bitPos = 0;                                                                                                                                                                          
				datum = 0;                                                                                                                                                                           
				decodeByteIndex = 0;                                                                                                                                                                 
				decodeData = data.GetEnumerator();                                                                                                                                                   
				decodeData.MoveNext();                                                                                                                                                               
				datum = decodeData.Current; // first byte to output                                                                                                                                  
			}                                                                                                                                                                                    

			/// <summary>                                                                                                                                                                            
			/// Read a single bit, appending trailing zeros as needed                                                                                                                                
			/// </summary>                                                                                                                                                                           
			/// <returns>The next bit</returns>                                                                                                                                                      
			public byte Read()                                                                                                                                                                       
			{                                                                                                                                                                                    
				byte bit = (byte)(datum >> 7);                                                                                                                                                       
				datum <<= 1;                                                                                                                                                                         
				++bitPos;                                                                                                                                                                            
				if (bitPos == 8)                                                                                                                                                                     
				{                                                                                                                                                                                
					decodeByteIndex++;                                                                                                                                                               
					if (decodeData.MoveNext())                                                                                                                                                       
						datum = decodeData.Current; // else allow to stay at 0                                                                                                                       
					bitPos = 0;                                                                                                                                                                      
				}                                                                                                                                                                                
				return bit;                                                                                                                                                                          
			}                                                                                                                                                                                    
			#endregion                                                                                                                                                                               

			#region Implementation                                                                                                                                                                   
			byte datum;                      // current byte of data                                                                                                                                 
			int bitPos;                   // the number of bits used from datum                                                                                                                      
			IEnumerator<byte> decodeData; // points to data being decoded                                                                                                                            
			long decodeByteIndex;         // index into data being decoded                                                                                                                           
			#endregion                                                                                                                                                                               
		}                                                                                                                                                                                        

		#endregion                                                                                                                                                                                   

	} // Compression namespace                                                                                                                                                                   
} // Lomont                                                                                                                                                                                      
// end of file                                                                                                                                                                                       



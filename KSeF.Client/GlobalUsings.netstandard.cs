// Guard class (Compatibility) is available globally on ALL TFMs:
// On netstandard2.0: provides polyfill implementations
// On net8.0+: provides inline forwarding to built-in methods
global using KSeF.Client.Compatibility;

#if NETSTANDARD2_0
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Security.Cryptography;
global using System.Security.Cryptography.X509Certificates;
global using System.Text;
#endif

// Klasa Guard (Compatibility) jest dostępna globalnie na wszystkich TFM:
// Na netstandard2.0: zapewnia implementacje polyfill
// Na net8.0+: zapewnia inline przekierowanie do wbudowanych metod
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

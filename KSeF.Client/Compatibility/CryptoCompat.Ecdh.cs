// ECDiffieHellman polyfill is NOT needed for netstandard2.0.
//
// The ECDiffieHellman abstract class is NOT part of the netstandard2.0 API surface.
// It exists only in .NET Core 3.0+ / .NET 5+.
//
// On .NET Framework 4.8, only the CNG-specific ECDiffieHellmanCng class exists,
// but it's not available through the netstandard2.0 compile-time contract.
//
// Resolution (FAZA 3): The CryptographyService.EncryptWithECDSAUsingPublicKey() method
// and any other code using ECDiffieHellman will be guarded with #if !NETSTANDARD2_0
// to exclude it from netstandard2.0 compilation. This is the correct approach because:
//   1. ECDiffieHellman-based encryption is a KSeF protocol feature that requires
//      runtime support for ECDH key agreement.
//   2. At runtime on .NET Framework 4.8, the ECDiffieHellmanCng class IS available,
//      but the netstandard2.0 compile target cannot reference it directly.
//   3. If .NET Framework 4.8 runtime support is needed in the future, a separate
//      #if NETFRAMEWORK path using ECDiffieHellmanCng could be added.

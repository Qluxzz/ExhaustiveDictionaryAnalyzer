# How to release

When you have done your changes:

1. Increase the version PackageVersion in ExhaustiveDictionary.Package/ExhaustiveDictionary.Package.csproj
2. Run `dotnet pack ExhaustiveDictionary.Package/ExhaustiveDictionary.Package.csproj -c Release`
3. Go to the nuget page and select the created ExhaustiveDictionary.Analyzer.X.X.X.nupkg file



when data base is setup in my sql run this 

commands 

dotnet ef migrations add InitialCreate
dotnet ef database update


If migrations fail, delete the Migrations folder and retry:

sh
Copy
Edit
rm -rf Migrations
dotnet ef migrations add InitialCreate
dotnet ef database update

## if permission error 
### xattr -d com.apple.quarantine /Users/shoebiqbal/Desktop/codeCompiler/Online_Assessment/OnlineAssessment.Web/bin/Debug/net8.0/OnlineAssessment.Web
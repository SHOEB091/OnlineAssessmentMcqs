/OnlineAssessment
│── /OnlineAssessment.Web/         # Main MVC Project (Frontend + API)
│   │── /Controllers/              # Handles HTTP Requests
│   │   │── AuthController.cs      # Login, Signup, JWT
│   │   │── UserController.cs      # User Management
│   │   │── OrganizationController.cs # Org Management
│   │   │── TestController.cs      # Test Start, Submit, Results
│   │   │── CodeExecutionController.cs # Code Compilation
│   │
│   │── /Views/                    # Razor Views (Frontend)
│   │   │── /Home/                 # Landing Page
│   │   │   │── Index.cshtml        # Test Details Page
│   │   │── /Auth/                 # Login/Signup UI
│   │   │   │── Login.cshtml
│   │   │   │── Signup.cshtml
│   │   │── /Test/                 # Test UI
│   │   │   │── StartTest.cshtml
│   │   │   │── MCQTest.cshtml
│   │   │   │── CodingTest.cshtml
│   │   │── /Shared/               # Common UI Components
│   │   │   │── _Layout.cshtml
│   │   │   │── _Navbar.cshtml
│   │   │   │── _Footer.cshtml
│   │
│   │── /Models/                   # Data Models (Entity Layer)
│   │   │── User.cs                # User Entity
│   │   │── Organization.cs         # Organization Entity
│   │   │── Test.cs                 # Test Entity
│   │   │── Question.cs             # MCQ Entity
│   │   │── CodingQuestion.cs       # Coding Question Entity
│   │   │── TestResult.cs           # Test Result Entity
│   │
│   │── /ViewModels/                # DTOs & View Models
│   │   │── LoginViewModel.cs
│   │   │── SignupViewModel.cs
│   │   │── TestViewModel.cs
│   │
│   │── /Services/                  # Business Logic Layer
│   │   │── AuthService.cs          # User Authentication
│   │   │── UserService.cs          # User Management
│   │   │── TestService.cs          # Test Handling Logic
│   │   │── CodeExecutionService.cs # Compiler Service
│   │
│   │── /Repositories/              # Data Access Layer
│   │   │── UserRepository.cs       # User Queries
│   │   │── TestRepository.cs       # Test Queries
│   │   │── QuestionRepository.cs   # MCQ Queries
│   │
│   │── /Middlewares/               # Middleware (Auth, Logging)
│   │   │── JwtMiddleware.cs        # JWT Authentication Middleware
│   │
│   │── /Configs/                   # Configurations
│   │   │── AppSettings.json        # App Configs (DB, JWT)
│   │
│   │── /wwwroot/                   # Static Assets (HTML, CSS, JS)
│   │   │── /css/                   # CSS Stylesheets
│   │   │── /js/                    # JavaScript Scripts
│   │   │── /images/                # Images & Icons
│   │
│   │── Program.cs                   # App Startup
│   │── Startup.cs                    # Middleware & Services Configuration
│
│── /OnlineAssessment.Compiler/      # Code Execution Service
│   │── /Compilers/                  # Compiler Handlers
│   │   │── PythonCompiler.cs
│   │   │── JavaCompiler.cs
│   │   │── CppCompiler.cs
│   │   │── JavaScriptCompiler.cs
│   │
│   │── /Docker/                     # Dockerized Containers
│   │   │── Dockerfile
│
│── /OnlineAssessment.Infrastructure/ # Database & Caching
│   │── /Migrations/                 # EF Core Migrations
│   │── /Database/                   # Database Configs
│   │── /Caching/                    # Redis Caching
│
│── /OnlineAssessment.MessageQueue/   # Kafka Event Processing
│   │── KafkaProducer.cs
│   │── KafkaConsumer.cs
│
│── /Tests/                          # Unit & Integration Tests
│   │── AuthServiceTests.cs
│   │── TestServiceTests.cs
│
│── docker-compose.yml                # Docker Setup
│── .gitignore                         # Git Ignore
│── README.md                          # Documentation

setup this in dotnet with vs code 

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
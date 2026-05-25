# 📦 Jig Inventory Management System

ระบบจัดการและติดตามสถานะของอุปกรณ์จิก (Jig) สำหรับกระบวนการผลิตในโรงงานอุตสาหกรรม ถูกออกแบบมาเพื่อตรวจสอบการเบิก-คืนจิก, สถานะการใช้งาน, สภาพของอุปกรณ์ และตำแหน่งจัดเก็บอย่างแม่นยำ

## 🌟 ฟีเจอร์หลัก (Key Features)
- **การจัดการข้อมูลจิก (Jig Management)**: บันทึกและจัดการข้อมูลจิก รวมถึงข้อมูล Smart Code (ToolNo, StepPrint, PartType ฯลฯ)
- **การติดตามสถานะและตำแหน่ง (Tracking & Locator System)**: ระบุตำแหน่งจัดเก็บจิกอย่างละเอียดตาม Site, ตู้ (Cabinet) และชั้น (Shelf) พร้อมระบุสถานะ (Available, InUse, Evaluation, Scrapped) และสภาพ (Good, NeedsCleaning, Broken)
- **ระบบสแกนรับ-จ่าย (Scan In / Scan Out)**: บันทึกประวัติการเบิก-คืนจิกแบบเรียลไทม์ (TransactionRow)
- **ระบบ Snapshot อัจฉริยะ**: บันทึกสถานะของจิกก่อนทำรายการ (JigStateSnapshot) เพื่อรองรับการย้อนกลับหรือยกเลิกรายการได้อย่างถูกต้อง 100%
- **การจัดการชิ้นส่วน (Part Management)**: เชื่อมโยงจิกเข้ากับหมายเลขชิ้นส่วน (Part Number) รูปแบบ Many-to-Many ผ่าน `JigPartMapping`
- **ระบบผู้ใช้งานและสิทธิ์ (Role-Based Access Control)**: รองรับระดับสิทธิ์ Admin, Engineer, ProdLead และ Operator
- **นำเข้าข้อมูลอัตโนมัติ (Excel Import)**: รองรับการนำเข้าฐานข้อมูลจิกเบื้องต้นผ่านไฟล์ Excel 
- **ระบบความปลอดภัย (Security)**: ใช้ JWT Authentication สำหรับตรวจสอบสิทธิ์ และมี API Rate Limiting ป้องกันการโจมตีหรือสแปม Request

## 🛠️ เทคโนโลยีที่ใช้ (Tech Stack)

**Frontend:**
- C# Blazor (Web App)
- TailwindCSS (Styling และ Utility-first CSS)
- NPM (สำหรับจัดการเครื่องมือ TailwindCSS)

**Backend:**
- ASP.NET Core Web API
- Entity Framework Core (ORM)
- Microsoft SQL Server (Database)
- JWT Authentication (Bearer Token)
- AspNetCoreRateLimit (API Rate Limiting)

**Shared:**
- C# Class Library (`shared.Models`) สำหรับใช้โมเดลข้อมูลร่วมกัน ลดความซ้ำซ้อนของโค้ด

---

## 📁 โครงสร้างโปรเจกต์แบบละเอียด (Detailed Project Structure)

โครงสร้างทั้งหมดเชื่อมโยงกันแบบต้นไม้ (Tree Structure) ดังนี้:

```text
Inventory-Management-System/
├── backend/                              # ฝั่งเซิร์ฟเวอร์ (API)
│   ├── Controllers/                      # จัดการ API Endpoints
│   │   ├── JigsController.cs             # API ข้อมูลจิกและสถานะ
│   │   ├── LocatorsController.cs         # API จัดการตำแหน่ง (Site, ตู้, ชั้น)
│   │   ├── TransactionsController.cs     # API บันทึกประวัติเบิก-คืน และสร้าง Snapshot
│   │   └── UsersController.cs            # API เข้าสู่ระบบ (Login) จัดการผู้ใช้
│   ├── Data/
│   │   └── AppDbContext.cs               # ศูนย์กลาง Entity Framework เพื่อติดต่อฐานข้อมูล
│   ├── Services/                         # ฝั่ง Business Logic
│   │   ├── ExcelImportService.cs         # นำเข้าข้อมูลจิกและชิ้นส่วนผ่านไฟล์ Excel
│   │   ├── JigService.cs                 # บริการสร้าง Smart Code
│   │   └── SeedDataService.cs            # สร้างข้อมูลเริ่มต้น (Seed Data)
│   ├── appsettings.json                  # คอนฟิกระบบ เช่น Connection Strings, JWT Key
│   └── Program.cs                        # จุดเริ่มต้นของ Backend, ตั้งค่า Middleware
├── frontend/                             # ฝั่งไคลเอนต์ (UI Blazor WebAssembly/Server)
│   ├── Components/
│   │   ├── Layout/                       # โครงร่างหน้าตาของแอป
│   │   │   ├── AppLayout.razor           # Layout หลักที่มีเมนูและแถบด้านข้าง
│   │   │   └── AuthLayout.razor          # Layout หน้าล็อกอิน
│   │   ├── Pages/                        # หน้าจอการใช้งานแต่ละเมนู
│   │   │   ├── Dashboard.razor           # ภาพรวมและสถิติ (UI)
│   │   │   ├── Dashboard.razor.cs        # โค้ดเบื้องหลัง (Code-behind)
│   │   │   ├── ScanOut.razor             # หน้าสแกนจิก (เบิก/คืน/แจ้งซ่อม)
│   │   │   ├── ScanOut.razor.cs          # โค้ดควบคุมลอจิกการสแกนและเรียก API
│   │   │   ├── CurrentStatus.razor       # ตารางเช็กสถานะจิกล่าสุด
│   │   │   ├── CurrentStatus.razor.cs    # โค้ดฟิลเตอร์ ค้นหา และดึงข้อมูลสถานะ
│   │   │   ├── History.razor             # ประวัติการทำรายการ (Transactions)
│   │   │   ├── History.razor.cs          # โค้ดดึงประวัติ แบ่งหน้าตาราง และค้นหา
│   │   │   ├── Profile.razor             # ตั้งค่าข้อมูลส่วนตัว
│   │   │   ├── Profile.razor.cs          # โค้ดบันทึกการแก้ไขโปรไฟล์
│   │   │   ├── Login.razor               # หน้าเข้าสู่ระบบ (หน้าแรก)
│   │   │   ├── LoginPassword.razor       # หน้าต่างกรอกรหัสผ่าน
│   │   │   └── JigOther/                 # เมนูการตั้งค่า Data Master
│   │   │       ├── RegisterJig.razor     # หน้าจัดการข้อมูลจิก (UI)
│   │   │       ├── RegisterJig.razor.cs  # โค้ดจัดการ CRUD สำหรับข้อมูลจิก
│   │   │       ├── Locators.razor        # หน้าจัดการตำแหน่งจัดเก็บจิก (UI)
│   │   │       ├── Locators.razor.cs     # โค้ดจัดการ CRUD สำหรับตำแหน่ง (Locator)
│   │   │       ├── Users.razor           # หน้าจัดการพนักงานและสิทธิ์ (UI)
│   │   │       ├── Users.razor.cs        # โค้ดจัดการระบบผู้ใช้งานและการให้สิทธิ์
│   │   │       ├── JigComponents/        # โมดอลย่อยสำหรับจิก (Form/Export)
│   │   │       ├── LocatorComponents/    # โมดอลย่อยสำหรับ Locator
│   │   │       └── UserComponents/       # โมดอลย่อยสำหรับ User
│   │   └── Shared/                       # คอมโพเนนต์ที่เรียกใช้ซ้ำได้
│   │       ├── Pagination.razor          # แบ่งหน้าตารางข้อมูล
│   │       ├── ReportIssueModal.razor    # โมดอลแจ้งปัญหา/แจ้งซ่อม
│   │       └── RoleGate.razor            # ควบคุมการแสดงผลตามสิทธิ์การใช้งาน
│   ├── Services/                         # การเรียกใช้งานต่างๆ ของฝั่ง UI
│   │   ├── ApiClientService.cs           # เรียกใช้งาน API ไปยัง Backend
│   │   ├── AuthService.cs                # เก็บค่า Token สิทธิ์ผู้ใช้งาน
│   │   ├── LanguageService.cs            # เปลี่ยนภาษา (TH/EN)
│   │   └── ThemeService.cs               # จัดการ Light/Dark Mode
│   ├── package.json                      # การตั้งค่า NPM 
│   ├── tailwind.config.js                # ตั้งค่า UI ของ TailwindCSS
│   ├── App.razor                         # คอมโพเนนต์รากของแอปพลิเคชัน
│   └── Program.cs                        # จุดเริ่มต้นของ Frontend ฝั่ง Web
└── shared/                               # โปรเจกต์ Models ส่วนกลาง
    └── Models.cs                         # เก็บ Data Model ทั้งหมด (Jig, User, Locator ฯลฯ)
```

---

## 🚀 การติดตั้งและการรันโปรเจกต์ (Getting Started)

### ข้อกำหนดเบื้องต้น (Prerequisites)
- [.NET SDK](https://dotnet.microsoft.com/download) (เวอร์ชัน 8.0 ขึ้นไป)
- [Node.js](https://nodejs.org/) (สำหรับรันและ Build TailwindCSS)
- Microsoft SQL Server (Express หรือเวอร์ชันอื่นๆ)

### 1. การตั้งค่า Backend
1. เปิดไฟล์ `backend/appsettings.json` และแก้ไข Connection String ในส่วนของ `DefaultConnection` ให้ตรงกับ SQL Server ของคุณ:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=.\\SQLEXPRESS;Initial Catalog=InventoryDB;User Id=YourUser;Password=YourPassword;Encrypt=False;TrustServerCertificate=True;"
   }
   ```
2. รันคำสั่งเพื่อสร้างและอัปเดตฐานข้อมูล (ถ้ายังไม่มี Migration ให้ใช้ `dotnet ef migrations add InitialCreate` ก่อน):
   ```bash
   cd backend
   dotnet ef database update
   ```
3. รันโปรเจกต์ Backend (Swagger UI จะรันขึ้นมาที่ `/swagger` ในโหมด Development):
   ```bash
   dotnet run
   ```

### 2. การตั้งค่า Frontend
1. เข้าไปที่โฟลเดอร์ frontend และติดตั้ง Node Modules สำหรับ Tailwind:
   ```bash
   cd frontend
   npm install
   ```
2. สั่งให้ Tailwind ทำการ Build และ Watch การเปลี่ยนแปลง CSS (คำสั่งนี้จะทำงานค้างไว้):
   ```bash
   npm run tw:watch
   ```
3. เปิด Terminal ใหม่อีกหน้าต่างเพื่อรันโปรเจกต์ Frontend:
   ```bash
   cd frontend
   dotnet run
   ```

*(หรือสามารถเปิดไฟล์ `Inventory-Management-System.sln` ใน Visual Studio และคลิกขวาที่ Solution > ตั้งค่า Multiple Startup Projects โดยให้รันทั้ง backend และ frontend พร้อมกันได้เลย)*

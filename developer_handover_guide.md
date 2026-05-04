# Developer Handover Guide: Jig Inventory Management System

เอกสารฉบับนี้จัดทำขึ้นเพื่อใช้ส่งมอบงาน (Handover) อย่างละเอียดที่สุด สำหรับนักพัฒนา (Developer) คนต่อไปที่จะเข้ามารับช่วงดูแลโปรเจกต์นี้ต่อ เอกสารนี้รวบรวมข้อมูลทุกอย่างตั้งแต่ ขอบเขตของระบบ การตั้งค่าโปรเจกต์ โครงสร้างเชิงลึก ระบบดีไซน์ และประวัติการพัฒนา

---

## 🎯 ส่วนที่ 1: ขอบเขตและฟีเจอร์ของโปรเจกต์ (Project Scope & Core Modules)

**เป้าหมายทางธุรกิจ (Business Goal):** 
เปลี่ยนผ่านระบบการจัดการจิ๊ก (อุปกรณ์ช่วยผลิต) จากการจดบันทึกด้วยมือหรือกระดาษ ให้เป็นระบบดิจิทัล 100% โดยอาศัยเทคโนโลยี **QR Code** ในการสแกนระบุตัวตนจิ๊กและระบุพิกัดชั้นวาง เพื่อลดความผิดพลาดและทำให้สามารถติดตามสถานะของจิ๊กได้แบบเรียลไทม์

### 👥 กลุ่มผู้ใช้งาน (Target Users & Roles)
1.  **Admin:** สามารถเข้าถึงได้ทุกเมนู จัดการข้อมูล JIG, จัดการชั้นวาง, ลบ/แก้ไขข้อมูลผู้ใช้งานในระบบ
2.  **Engineer:** มีสิทธิ์หลักในการ **จัดการข้อมูล JIG** (เพิ่ม/แก้ไข/ลบ/Import) แต่ในส่วนของ Locator จะทำได้แค่ **"ดูและปรินต์ QR Code"** เท่านั้น (ไม่สามารถแก้พิกัดชั้นวางได้)
3.  **ProdLead (หัวหน้าฝ่ายผลิต):** มีสิทธิ์หลักในการ **จัดการพิกัดชั้นวาง Locator** (เพิ่ม/แก้ไข/ลบ) แต่ในส่วนของข้อมูล JIG จะทำได้แค่ **"ดูและปรินต์ QR Code"** เท่านั้น (ไม่สามารถแก้ไขสเปก JIG ได้)
4.  **Operator (ผู้ปฏิบัติงานทั่วไป):** มีสิทธิ์จำกัด สามารถทำได้แค่ "สแกนเบิก (Check Out)", "สแกนคืน (Check In)" และดูประวัติการสแกนของตัวเองเท่านั้น
5.  **Guest (ผู้เยี่ยมชม):** สามารถเข้าสู่ระบบได้โดยไม่ต้องใช้รหัสผ่าน มีสิทธิ์แบบ **Read-Only** คือดูได้อย่างเดียว (เช่น ดูสถานะใน Dashboard) ไม่สามารถทำรายการสแกนเบิก-คืน ไม่สามารถดูประวัติ และไม่สามารถแก้ไขอะไรในระบบได้เลย

### 🧩 โมดูลหลักของระบบ (Core Modules)
ระบบถูกแบ่งการทำงานออกเป็น 5 โมดูลหลักๆ ดังนี้:

**1. Jig Management Module (ระบบจัดการข้อมูลจิ๊ก)**
*   เพิ่ม แก้ไข ลบ ข้อมูลของจิ๊กแต่ละตัว
*   รองรับการอัปโหลดรูปภาพของจิ๊กแต่ละตัว (เก็บรูปลงในโฟลเดอร์ ไม่ได้เก็บลง DB)
*   **Import Excel:** สามารถโยนไฟล์ Excel แผนการผลิตเข้าไป เพื่อให้ระบบสร้างข้อมูลจิ๊กและสร้างรหัส ID (เช่น 20471-01) หรือ Smart Code อัตโนมัติ
*   **Export Data:** สามารถส่งออกข้อมูลจิ๊กทั้งหมดออกมาในรูปแบบไฟล์ `.CSV` (สำหรับไปทำ Data Data Analytics) หรือ `.PDF` (สำหรับปรินต์รายงาน)
*   สั่งปรินต์ QR Code ของจิ๊กทีละตัวหรือทีละหลายๆ ตัวได้

**2. Locator Management Module (ระบบจัดการตำแหน่งจัดเก็บ)**
*   สร้างและระบุพิกัดชั้นวาง (Site, Cabinet, Shelf)
*   สร้าง QR Code ให้กับชั้นวาง เพื่อใช้ประกอบการสแกนคืนจิ๊กเข้าชั้น
*   ดูได้ว่าชั้นวางแต่ละชั้น ปัจจุบันมีจิ๊กตัวไหนถูกจัดเก็บอยู่บ้าง

**3. Transaction / Scan Workflow Module (ระบบสแกนเบิก-คืน)**
*   **Scan Out (เบิกออก):** ผู้ใช้งานสแกนพนักงานตนเอง ➜ สแกน QR บนตัวจิ๊ก ➜ ระบบเปลี่ยนสถานะจิ๊กเป็น "In Use"
*   **Scan In (ส่งคืน):** ผู้ใช้งานสแกน QR บนตัวจิ๊ก ➜ สแกน QR ประจำชั้นวาง (Locator) ➜ ระบบบันทึกพิกัดชั้นวาง และเปลี่ยนสถานะจิ๊กเป็น "Available" พร้อมอัปเดตสภาพการใช้งาน (Good, Needs Cleaning, Broken)

**4. Reporting & Dashboard (ระบบรายงาน)**
*   หน้า Dashboard สรุปจำนวนจิ๊กทั้งหมด แบ่งตามสถานะ (Available, In Use, Scrapped) และสัดส่วนสภาพการใช้งาน
*   หน้า Transactions (History) สำหรับดูประวัติ (Log) ของการเคลื่อนไหวจิ๊กทั้งหมด ว่าใครเป็นคนเบิกไป เวลาไหน ส่งคืนเมื่อไหร่

**5. User Management (ระบบจัดการผู้ใช้)**
*   สมัคสมาชิก และล็อกอินเข้าสู่ระบบด้วยรหัสผ่าน (Hash ด้วย BCrypt)
*   รองรับระบบ 2 ภาษา (EN / TH)

---

## 📘 ส่วนที่ 2: README (วิธี Setup และวิธี Run โปรเจกต์)

### 🛠️ สิ่งที่ต้องติดตั้ง (Prerequisites)
*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express หรือ Developer)
*   SQL Server Management Studio (SSMS) สำหรับดูข้อมูล
*   Node.js (สำหรับการรัน Tailwind CSS ในกรณีที่ต้องการเปลี่ยนดีไซน์)

### 🚀 วิธีการรันโปรเจกต์บนเครื่อง Development

**1. การตั้งค่า Database**
*   เปิดไฟล์ `backend/appsettings.json` และ `backend/appsettings.Development.json`
*   แก้ค่า `DefaultConnection` ให้ชี้ไปที่ SQL Server ในเครื่องของคุณ (เช่น `Server=(local)\\SQLEXPRESS;Database=InventoryDB;Trusted_Connection=True;TrustServerCertificate=True;`)
*   เปิด Terminal ไปที่โฟลเดอร์ `backend` แล้วรันคำสั่ง `dotnet ef database update` เพื่อสร้างตาราง

**2. การรัน Backend (API)**
```bash
cd backend
dotnet run --launch-profile "https"
```
*(ระบบ Backend จะทำงานที่ `https://localhost:7120` หรือพอร์ตที่กำหนดใน `launchSettings.json`)*

**3. การรัน Frontend (Web UI)**
*   ตรวจสอบไฟล์ `frontend/Program.cs` บรรทัด `BaseAddress` ว่าชี้ไปที่ IP/Port ของ Backend ถูกต้อง
```bash
cd frontend
dotnet run
```
*(ระบบจะเปิดหน้าเว็บที่ `http://localhost:5196` หรือ `https://localhost:7196`)*

---

## 🏗️ ส่วนที่ 3: ARCHITECTURE (โครงสร้างสถาปัตยกรรมระบบ)

ระบบนี้ใช้สถาปัตยกรรมแบบ **3-Tier Architecture** โดยแยกฝั่ง Client และ Server ออกจากกัน 100% เพื่อให้รองรับการสเกลในอนาคต

### 🖥️ 3.1 Frontend (หน้าบ้าน)
*   **Framework:** Blazor WebAssembly (.NET 8) ทำงานบนเบราว์เซอร์ของ User โดยตรง (SPA - Single Page Application)
*   **Routing:** ใช้ระบบ Routing ของ Blazor (`@page "/admin/register-jig"`)
*   **Authentication:** ใช้ระบบ JWT Token เก็บใน `localStorage` นำไปแนบใน Header (`Authorization: Bearer <token>`) ทุกครั้งที่เรียก API
*   **Print Engine:** การสั่งพิมพ์ QR Code ไม่ได้ใช้ไลบรารีพิเศษ แต่อาศัยฟังก์ชัน JavaScript เพียวๆ ฝังอยู่ใน `wwwroot/index.html` (ฟังก์ชัน `printLocatorQR()`) โดยฝั่ง Blazor C# จะโยนข้อมูล HTML ของรูป QR ผ่าน `JSRuntime.InvokeVoidAsync` ไปให้ JavaScript สั่งพิมพ์

### ⚙️ 3.2 Backend (หลังบ้าน)
*   **Framework:** ASP.NET Core Web API (.NET 8)
*   **Controllers:** 
    *   `AuthController`: จัดการล็อกอิน, ออก JWT Token
    *   `JigsController`: CRUD ข้อมูลจิ๊ก, อัปโหลดรูปภาพ, และ **ฟีเจอร์นำเข้า Excel (`UploadExcel`)** 
    *   `LocatorsController`: จัดการชั้นวาง และผูกพิกัด
    *   `TransactionsController`: API บันทึกการแสกน
*   **File Storage:** รูปภาพที่อัปโหลดจะเก็บไว้ใน Directory จริงที่โฟลเดอร์ `backend/wwwroot/uploads/` และเก็บแค่ URL สั้นลงใน DB

### 🗄️ 3.3 Database (ฐานข้อมูล)
*   **ORM:** Entity Framework Core (Code-First Approach)
*   **ตารางหลัก (Key Tables):**
    *   `Jigs`: เก็บ Specs (Tool No, Step Print, Part Type) คอลัมน์ `Id` คือ Primary Key
    *   `Locators`: เก็บพิกัด (Site, Cabinet, Shelf)
    *   `Transactions`: ตาราง Log บันทึกประวัติสแกน
    *   `UserAccounts`: เก็บ Username, Password Hash, Role
    *   `PartMasters` & `JigPartMappings`: เก็บความสัมพันธ์ว่าจิ๊กตัวไหน ใช้กับ Part Number ตัวไหนได้บ้าง (Many-to-Many)

---

## 🎨 ส่วนที่ 4: DESIGN SYSTEM (ระบบดีไซน์)

ระบบนี้ไม่ได้ใช้ UI Component สำเร็จรูปอย่าง MudBlazor หรือ Bootstrap แต่ใช้ **Tailwind CSS** แบบ 100% ในการควบคุมหน้าตา เพื่อความยืดหยุ่นและให้ได้ความสวยงามระดับพรีเมียม (Premium Dark Theme)

### 🖌️ 4.1 สีหลัก (Color Palette)
*   **Mattel Brand Color:** `Red: #E5002B` (ใช้เป็นปุ่ม Action หลัก และ Navbar) / Hover `Red: #C40024`
*   **Dark Theme (Slate):**
    *   พื้นหลังเว็บ: `bg-slate-950`
    *   พื้นหลังกล่อง (Card): `bg-slate-900` หรือ `bg-slate-800`
    *   เส้นขอบ: `border-slate-700`
    *   ข้อความหลัก: `text-slate-200`
    *   ข้อความรอง: `text-slate-400`
*   **Status Badges:**
    *   🟢 Available: `bg-green-900/30 text-green-400`
    *   🔵 In Use: `bg-blue-900/30 text-blue-400`
    *   🔴 Scrapped: `bg-red-900/30 text-red-400`

### 🧩 4.2 Components มาตรฐาน
*   **Buttons:** ขอบมน `rounded-xl`, มี transition `transition-colors`, เน้นใช้สี Mattel Red ตัดกับพื้นหลังสีเข้ม
*   **Inputs / Selects:** พื้นหลังทึบ `bg-slate-900`, ขอบบาง `border border-slate-700`, เลื่อนคลิกจะเปล่งแสง `focus:ring-1 focus:ring-mattel-red`
*   **Modals (Popup):** มีพื้นหลังดำโปร่งแสง `bg-slate-900/50 backdrop-blur-sm` ให้ความรู้สึก Glassmorphism

### 🖨️ 4.3 Print Layouts (ใบปรินต์คิวอาร์โค้ด)
*   เขียนด้วย HTML/CSS ธรรมดาในหน้าต่าง Popup ใหม่เวลาสั่งพิมพ์
*   ใช้ `@page { size: A4; margin: 15mm; }`
*   การปรับขนาดตัวอักษรและรูปภาพคิวอาร์ (ล่าสุดอยู่ที่ QR=150px, Text=20pt) จะต้องแก้ไขในไฟล์ `frontend/wwwroot/index.html` เท่านั้น

---

## 📜 ส่วนที่ 5: CHANGELOG (ประวัติการเปลี่ยนแปลงที่สำคัญ)

*   **[Tag 101 - 200] การวางรากฐานระบบ:**
    *   สร้างระบบ Backend, Database และทำ Authentication (JWT)
    *   สร้าง UI Blazor แบบ Dark Theme
*   **[Tag 301 - 304] ระบบ Import และ Layouts:**
    *   เพิ่มระบบอัปโหลดไฟล์ Excel เพื่อ Import ข้อมูล Jigs
    *   ปรับปรุง Navbar ให้ใหญ่ขึ้น 1.5 เท่า
    *   เพิ่มฟีเจอร์แสดง/ซ่อนรหัสผ่านในหน้า Login และ Profile
*   **[Tag 401 - Current] ระบบ QR Code และ Locators:**
    *   เพิ่มระบบการจัดการ Locator (ชั้นวาง)
    *   สร้างระบบพิมพ์ QR Code (ใช้ JS ฝังใน index.html)
    *   อัปเดตขนาดใบพิมพ์ QR Code ให้พอดีกับการแปะตู้ Locator และแก้ปัญหาข้อความ ID ยาวทะลุกรอบกระดาษ

---

## ⚠️ ส่วนที่ 6: ข้อควรระวังและข้อห้าม (Important Warnings & Best Practices)

ฝากถึง Developer คนต่อไป กรุณาอ่านและปฏิบัติตามกฎเหล็กเหล่านี้เพื่อรักษามาตรฐานและป้องกันโครงสร้างระบบพัง:

> [!WARNING]
> **1. การแยกส่วน Frontend และ Backend อย่างเด็ดขาด (Strict Separation)**
> โปรเจกต์นี้ถูกออกแบบมาแบบ 3-Tier Architecture ที่ **แยกหน้าบ้านและหลังบ้านออกจากกันโดยสิ้นเชิง** 
> *   **ห้าม** นำโค้ดที่เชื่อมต่อ Database (เช่น Entity Framework, SQL Query) หรือ Business logic หนักๆ ไปเขียนฝังลงในไฟล์ `.razor` (Frontend) เด็ดขาด 
> *   การรับส่งข้อมูลทุกอย่างของหน้าเว็บจะต้องทำผ่านการยิง API (`HttpClient`) ไปหาฝั่ง Backend เท่านั้น
> *   หากหน้าเว็บต้องการดึงข้อมูลอะไรเพิ่มเติม **ให้ไปสร้าง Endpoint API (Controller) ขึ้นมาใหม่ที่ฝั่ง Backend** แล้วค่อยให้หน้าเว็บเป็นคนเรียกใช้เสมอ

> [!IMPORTANT]
> **2. การสั่งพิมพ์ QR Code และระบบแคช (Browser Cache)**
> โค้ดที่ควบคุมหน้าตา ขนาดกระดาษ และโครงสร้างของการสั่งพิมพ์ QR Code (Print Layout) ถูกเขียนด้วย Javascript/CSS ธรรมดา และฝังอยู่ในไฟล์ `frontend/wwwroot/index.html` 
> *   หากคุณมีการแก้ไขโค้ดส่วนนี้ เบราว์เซอร์มักจะจดจำ Cache เดิมไว้เสมอ ทำให้แก้โค้ดแล้วปริ้นออกมาเหมือนเดิม **คุณต้องกด `Ctrl + F5` (Hard Reload) ทุกครั้งหลังแก้โค้ด** เพื่อล้างแคช

> [!TIP]
> **3. การตกแต่ง UI ด้วย Tailwind CSS**
> พยายามหลีกเลี่ยงการเขียน CSS แบบ `<style>` ฝังเข้าไปใน Component หากไม่จำเป็น ขอให้ใช้คลาสของ Tailwind CSS แทน (เช่น `bg-slate-900`, `text-mattel-red`) เพื่อรักษาให้ธีมและโทนสีของเว็บไซต์ (Design System) เป็นไปในทิศทางเดียวกันทั้งหมด

---

## 📁 ส่วนที่ 7: โครงสร้างไฟล์ทั้งหมดแบบละเอียด (Full Directory Structure)

แผนผังด้านล่างนี้คือรายการไฟล์โค้ดที่สำคัญทั้งหมด (คัดเอา `bin`, `obj`, `node_modules` ออกแล้ว) เพื่อให้ Developer คนต่อไปหาไฟล์ได้รวดเร็ว:

```text
Inventory-Management-System/
├── Inventory-Management-System.sln      ← ไฟล์ Solution สำหรับเปิดใน Visual Studio
├── package.json                         ← ไฟล์คอนฟิก Node.js (สำหรับ Tailwind)
├── tailwind.config.js                   ← ไฟล์ตั้งค่าสไตล์ Tailwind CSS
├── README.md                            ← เอกสารหน้าแรกของ Repository
├── ARCHITECTURE.md                      ← เอกสารอธิบายระบบเชิงลึก (แยกต่างหาก)
├── DESIGN_SYSTEM.md                     ← เอกสารระบบดีไซน์ (แยกต่างหาก)
├── CHANGELOG.md                         ← ประวัติการแก้ไขเวอร์ชัน (แยกต่างหาก)
│
├── shared/                              ← โปรเจกต์เก็บคลาสที่ใช้ร่วมกัน
│   ├── shared.csproj
│   └── Models.cs                        ← โมเดลข้อมูลที่ใช้แชร์ระหว่างฝั่งหน้าบ้านและหลังบ้าน
│
├── scripts/                             ← สคริปต์ตัวช่วยต่างๆ
│   └── serve-frontend-local.sh
│
├── backend/                             ← โปรเจกต์ API (ASP.NET Core)
│   ├── backend.csproj
│   ├── Program.cs                       ← จุดเริ่มต้นการตั้งค่า Backend (CORS, JWT, DB)
│   ├── appsettings.json                 ← ไฟล์ตั้งค่าหลัก (ConnectionString, JWT Key)
│   ├── appsettings.Development.json     ← ไฟล์ตั้งค่าสำหรับตอน Dev
│   ├── Controllers/                     ← โฟลเดอร์รวม API Endpoints
│   │   ├── AuthController.cs            ← API สำหรับ Login / สร้าง User
│   │   ├── JigsController.cs            ← API จัดการ JIG (รวมฟังก์ชัน Import Excel)
│   │   ├── LocatorsController.cs        ← API จัดการชั้นวาง (Locators)
│   │   ├── TransactionsController.cs    ← API จัดการประวัติเบิก-คืน
│   │   └── UsersController.cs           ← API จัดการข้อมูล User
│   ├── Data/
│   │   ├── AppDbContext.cs              ← ไฟล์ตั้งค่า Entity Framework และความสัมพันธ์ตาราง
│   │   └── Migrations/                  ← โฟลเดอร์เก็บประวัติการอัปเดต Database
│   ├── Models/                          ← โฟลเดอร์รวม Database Schema (ตารางใน SQL)
│   │   ├── Jig.cs
│   │   ├── JigPartMapping.cs
│   │   ├── Locator.cs
│   │   ├── PartMaster.cs
│   │   ├── Transaction.cs
│   │   └── UserAccount.cs
│   ├── Properties/
│   │   └── launchSettings.json          ← ตั้งค่า Port เวลา Run
│   └── wwwroot/
│       └── uploads/                     ← เก็บรูปภาพจิ๊กที่อัปโหลด
│
└── frontend/                            ← โปรเจกต์ Web UI (Blazor WebAssembly)
    ├── frontend.csproj
    ├── Program.cs                       ← จุดเริ่มต้นการตั้งค่า Frontend (ชี้เป้า BaseAddress ไปหา Backend)
    ├── App.razor                        ← จุดเริ่มต้น Component
    ├── _Imports.razor                   ← รวมคำสั่ง using namespace ต่างๆ
    ├── Components/                      ← โฟลเดอร์รวมหน้าจอทั้งหมด
    │   ├── Layout/
    │   │   ├── MainLayout.razor         ← โครงสร้างหลัก (Navbar, Sidebar)
    │   │   ├── MainLayout.razor.css     
    │   │   ├── NavMenu.razor            ← เมนูด้านข้าง
    │   │   └── NavMenu.razor.css
    │   ├── Pages/                       ← โฟลเดอร์ย่อยรวมหน้าจอต่างๆ
    │   │   ├── Admin/
    │   │   │   ├── Locators.razor       ← หน้าจอจัดการ Locator
    │   │   │   ├── RegisterJig.razor    ← หน้าจอลงทะเบียน JIG / Import Excel
    │   │   │   ├── Transactions.razor   ← หน้าจอดูประวัติการสแกนทั้งหมด
    │   │   │   └── Users.razor          ← หน้าจอจัดการผู้ใช้งานระบบ
    │   │   ├── Dashboard.razor          ← หน้าจอสรุปข้อมูล/กราฟ
    │   │   ├── History.razor            ← หน้าจอประวัติการสแกน (ของ User ตัวเอง)
    │   │   ├── Home.razor               ← หน้าหลักหลัง Login (สแกน Check In)
    │   │   ├── Login.razor              ← หน้าจอ Login (กรอก Username)
    │   │   ├── LoginPassword.razor      ← หน้าจอ Login (กรอก Password)
    │   │   ├── Profile.razor            ← หน้าจอโปรไฟล์ส่วนตัว
    │   │   ├── ScanOut.razor            ← หน้าจอสแกน Check Out
    │   │   └── Error.razor              
    │   └── Shared/
    │       ├── PageSizeSelector.razor   ← Component เลือกจำนวนแถวต่อหน้า
    │       ├── Pagination.razor         ← Component เปลี่ยนหน้า (1,2,3...)
    │       └── RoleGate.razor           ← Component สำหรับกั้นสิทธิ์การเข้าถึงหน้าจอ
    ├── Properties/
    │   └── launchSettings.json          
    ├── Services/
    │   ├── AuthService.cs               ← จัดการคุกกี้ / LocalStorage / JWT
    │   └── LanguageService.cs           ← จัดการระบบ 2 ภาษา (EN/TH)
    ├── Styles/
    │   └── tailwind.css                 ← ไฟล์ต้นทาง Tailwind (ถูกแปลงไปใส่ wwwroot/css/app.css)
    └── wwwroot/                         ← โฟลเดอร์รวมไฟล์ Public (รูป, CSS, JS)
        ├── index.html                   ← **[สำคัญ]** โค้ด JavaScript สำหรับสั่ง Print QR Code อยู่ที่นี่
        ├── favicon.png
        ├── icon-192.png
        ├── css/
        │   ├── app.css                  ← ไฟล์ CSS หลักที่ถูก Generate แล้ว
        │   └── layout-responsive.css
        └── images/
            └── mattel.png               ← รูปโลโก้บริษัท
```

---

> 💻 **Developer Credit:** 
> *ระบบนี้ได้รับการออกแบบ พัฒนา และจัดทำเอกสารเบื้องต้นโดย **JAMESBOND 007**.* 
> *ขอขอบคุณที่ดูแลรักษาโครงการนี้!*

# 📦 Inventory Management System

ระบบเว็บสำหรับ **ติดตาม จัดการ และควบคุมอุปกรณ์ Jig และเครื่องมือในกระบวนการผลิต** ภายในสภาพแวดล้อมอุตสาหกรรม

ระบบนี้ถูกออกแบบมาเพื่อ **ทดแทนการจัดการข้อมูลแบบ Excel หรือ Spreadsheet แบบเดิม** โดยพัฒนาเป็นแพลตฟอร์มเว็บที่ช่วยให้สามารถติดตามสถานะอุปกรณ์ได้แบบเรียลไทม์ พร้อมระบบกำหนดสิทธิ์ผู้ใช้งาน การสแกน QR Code และระบบบันทึกประวัติการใช้งานเพื่อการตรวจสอบย้อนหลัง

พัฒนาด้วยเทคโนโลยี **Blazor Web App, Entity Framework Core, Microsoft SQL Server และ Tailwind CSS**

---

# 🌟 ความสามารถหลักของระบบ

### 🔐 ระบบกำหนดสิทธิ์ผู้ใช้งาน (Role-Based Access Control)

ระบบรองรับการกำหนดสิทธิ์ผู้ใช้งานตามบทบาท เพื่อควบคุมการเข้าถึงข้อมูลและการดำเนินการต่าง ๆ ภายในระบบ

บทบาทผู้ใช้งานประกอบด้วย

* Admin
* Engineer
* Production Lead
* Operator
* Guest

แต่ละบทบาทจะกำหนดว่า ผู้ใช้สามารถ

* ดูข้อมูล
* แก้ไขข้อมูล
* จัดการระบบ

ได้ในระดับใด

---

### 🗂 การจัดการข้อมูลหลักของระบบ (Master Data Management)

ระบบจัดเก็บข้อมูลพื้นฐานที่ใช้เป็นโครงสร้างของระบบทั้งหมด

**Jig Specifications (Jig Specs)**
เก็บข้อมูลสเปกและรายละเอียดของ Jig แต่ละประเภท

**Locations**
ระบบตำแหน่งจัดเก็บแบบลำดับชั้น

```text
Site → Cabinet → Shelf → Position
```

ใช้สำหรับระบุ **ตำแหน่งจัดเก็บจริงของอุปกรณ์**

**Part Mapping (BOM)**
เชื่อมโยงระหว่าง

* ชิ้นส่วนการผลิต (Part)
* Jig Specification ที่ต้องใช้

---

### 🔧 การติดตามอุปกรณ์จริง (Physical Asset Tracking)

ระบบสามารถติดตาม **Jig แต่ละตัวที่ใช้งานจริงในสายการผลิต**

ข้อมูลของ Physical Jig ประกอบด้วย

* รหัสประจำ Jig
* สถานะปัจจุบัน (Available / In Use / Maintenance)
* สภาพอุปกรณ์
* ตำแหน่งจัดเก็บ
* การเชื่อมโยงกับ Jig Specification

---

### 📊 การนำเข้าข้อมูลผ่าน Excel และ CSV

ระบบรองรับการนำเข้าข้อมูลจำนวนมากผ่านไฟล์

* `.xlsx`
* `.csv`

ใช้สำหรับการเพิ่มข้อมูล เช่น

* Jig Specifications
* Locations
* Part Mappings
* Physical Jig Inventory

ช่วยลดเวลาการกรอกข้อมูลจำนวนมาก

---

### 📱 ระบบสร้าง QR Code

ระบบสามารถสร้าง **QR Code สำหรับ Physical Jig แต่ละตัว**

ประโยชน์ของระบบนี้

* สแกนเพื่อค้นหาอุปกรณ์ได้ทันที
* ลดเวลาในการค้นหา Jig
* เพิ่มความรวดเร็วในการทำงานบนพื้นที่การผลิต

---

### 🎨 ส่วนติดต่อผู้ใช้งานสมัยใหม่ (Modern UI)

พัฒนา UI ด้วย **Tailwind CSS**

ข้อดี

* รองรับการแสดงผลหลายขนาดหน้าจอ
* โหลดหน้าเว็บรวดเร็ว
* อินเทอร์เฟซใช้งานง่าย

---

# 🏗️ โครงสร้างโปรเจค

โปรเจคถูกออกแบบให้แยกส่วน **UI, Business Logic และ Data Access** ออกจากกันอย่างชัดเจน

```text
📂 Inventory
│
├── 📂 Components
│   ├── 📂 Layout
│   │   └── โครงสร้าง Layout และเมนูหลักของระบบ
│   │
│   ├── 📂 Pages
│   │   ├── 📂 Admin
│   │   │   ├── JigSpecs
│   │   │   ├── PhysicalJigs
│   │   │   └── Locations
│   │   │
│   │   └── หน้าสำหรับผู้ใช้งาน
│   │       ├── Dashboard
│   │       ├── Login
│   │       └── History
│   │
│   ├── 📂 Shared
│   │   └── Components ที่ใช้ซ้ำได้
│   │
│   ├── App.razor
│   └── Routes.razor
│
├── 📂 Models
│   └── Entity classes ที่กำหนดโครงสร้าง Database
│
├── 📂 Services
│   ├── AuthService.cs
│   ├── ExcelImportService.cs
│   └── SeedDataService.cs
│
├── 📂 Data
│   └── AppDbContext (Entity Framework Core)
│
├── 📂 wwwroot
│   └── Static files เช่น CSS / JS / Images
│
├── appsettings.json
│
└── Program.cs
```

---

# 🏛️ สถาปัตยกรรมของระบบ (System Architecture)

ระบบถูกออกแบบตามแนวทาง **Layered Architecture** เพื่อให้สามารถพัฒนาและดูแลรักษาระบบได้ง่าย

```text
Users
 │
 ▼
Web Browser
 │
 ▼
Blazor Web UI
 │
 ▼
Application Services
 │
 ▼
Entity Framework Core
 │
 ▼
SQL Server Database
```

---

# 🚀 เทคโนโลยีที่ใช้ในระบบ

### Backend

* .NET 8 / .NET 9
* ASP.NET Core
* Entity Framework Core

### Frontend

* Blazor Web App

### Styling

* Tailwind CSS

### Database

* Microsoft SQL Server

### Data Processing

* ExcelDataReader

### QR Code Generation

* Net.Codecrete.QrCodeGenerator

---

# 💡 การเริ่มต้นใช้งานระบบ

### 1️⃣ สิ่งที่ต้องติดตั้งก่อน

* .NET SDK
* SQL Server

---

### 2️⃣ ตั้งค่า Database

แก้ไข Connection String ในไฟล์

`appsettings.json`

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=InventoryDB;Trusted_Connection=True;"
}
```

---

### 3️⃣ รันระบบ

```bash
dotnet run --urls "http://localhost:5101"
```

---

### 4️⃣ บัญชีเริ่มต้นของระบบ

```text
Username: admin
Password: admin
```

*(แนะนำให้เปลี่ยนรหัสผ่านหลังจากเข้าใช้งานครั้งแรก)*

---

# 📸 ภาพตัวอย่างระบบ

สามารถเพิ่มภาพหน้าจอของระบบในส่วนนี้ได้ เช่น

* Dashboard
* Jig Inventory
* QR Scan Page
* Import Data Page

---

# 🎯 การนำระบบไปใช้งาน

ระบบนี้เหมาะสำหรับ

* การจัดการเครื่องมือในสายการผลิต
* ระบบติดตาม Jig ในโรงงาน
* การจัดการอุปกรณ์การผลิต
* ระบบ Inventory สำหรับอุตสาหกรรม

---

# 📄 License

สำหรับการศึกษาและ Portfolio

---

# 👨💻 ผู้พัฒนา

นักศึกษาวิศวกรรมคอมพิวเตอร์
สนใจด้าน **Industrial Systems, Inventory Tracking และ Manufacturing Automation**

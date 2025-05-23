# DotFetch.NET

A fast, minimal neofetch-like system info tool written in **C# (.NET 8)** for Windows terminals.

Displays your system's basic info alongside a colored ASCII logo, cleanly aligned side-by-side.

---

## Features

- User and Computer name  
- OS and Kernel version  
- Host name and uptime  
- Shell and terminal  
- CPU, GPU, and RAM info  
- Disk usage  
- Battery status  
- Internet IP and connectivity  
- Admin role detection  
- Neatly aligned ASCII logo  
- Clean output without external dependencies

---

## Sample Output

```
    ███        ███   Nahian@DESKTOP
    ███        ███   ----------------
    ███        ███   Windows 11 Pro
    ███        ███   10.0.22631.3447
    ███        ███   Lenovo Legion 5
    ███        ███   Uptime: 3h 22m
    ███        ███   Shell: pwsh.exe
    ███        ███   Terminal: Windows Terminal
    ███        ███   CPU: AMD Ryzen 7 5800H
    ███        ███   GPU: RTX 3060 Laptop GPU
    ███        ███   RAM: 12.3 GB / 16 GB
    ███        ███   Drive: C:\ 320 GB free / 476 GB
    ███        ███   Admin: Yes
    ███        ███   Internet: Connected
    ███        ███   IP: 103.92.24.5
    ███        ███   Battery: 81% (Plugged in)
```

---

## Build & Run

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Windows 10 or higher

### Instructions

```bash
git clone https://github.com/evilprince2009/DotFetch.NET.git
cd DotFetch.NET
dotnet run --project DotFetch.NET
```

---

## Customization

- Edit ASCII logo in `LogoRenderer.cs`
- Modify info fields in `InformationGenerator.cs`
- Output alignment handled in `Combiner.cs`

---

## Author

**Ibne Nahian**  
[GitHub: evilprince2009](https://github.com/evilprince2009)  

> Coded with ♥ in PowerShell, C# and a love for clean CLI tools.

---

## License

MIT

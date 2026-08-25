# Inquires — מערכת ניהול פניות

פרויקט בחינה Full Stack: שרת API ב-ASP.NET Core (4 פרויקטים בשכבות) + לקוח Angular, עם SQL Server כבסיס נתונים.

## 1. הוראות הרצה

### בסיס נתונים
1. יש SQL Server / LocalDB זמין (מחרוזת החיבור המוגדרת: `(localdb)\mssqllocaldb`, מסד `InquiresDb`).
2. הרצת הסקריפטים לפי הסדר (למשל דרך SSMS או `sqlcmd`):
   - `Database/Schema.sql` — יוצר את הטבלאות `Statuses`, `Priorities`, `Inquiries` (כולל אינדקסים).
   - `Database/Seed.sql` — מזין כ-10,000 רשומות לדוגמה.

### שרת (API)
```
cd Inquires.Api
dotnet run
```
ה-API עולה על `http://localhost:5120` (וגם `https://localhost:7245`). ב-Development נגיש Swagger UI מהשורש.

### לקוח (Angular)
```
cd inquires-client
npm install
npm start
```
האפליקציה עולה על `http://localhost:4200` ומדברת עם ה-API דרך `http://localhost:5120/api` (מוגדר ב-`src/app/core/api-config.ts`).

### בדיקות (Unit Tests)
```
dotnet test Inquires.Tests
```

## 2. טכנולוגיות בשימוש

- **.NET 10 / ASP.NET Core Web API** — שכבת השרת.
- **Entity Framework Core 10 + SQL Server** — גישה לנתונים, בגישת Database-First.
- **Angular 22 + Angular Material + RxJS** — שכבת הלקוח.
- **IMemoryCache** — קאשינג בזיכרון בשכבת ה-Services.
- **xUnit** — בדיקות יחידה.
- **Swagger / OpenAPI** — תיעוד ובדיקה ידנית של ה-API בסביבת פיתוח.

## 3. מבנה הפתרון

השרת בנוי כ-4 פרויקטים עם כיוון תלות חד-כיווני:

```
Inquires.Api  →  Inquires.Services  →  Inquires.Data  →  Inquires.DTO
```

- **Inquires.Data** — `DbContext`, ישויות (`Inquiry`, `Status`, `Priority`), Repository לגישה לנתונים.
- **Inquires.DTO** — מודלים של Request/Response ומיפויים בין Entity ל-DTO.
- **Inquires.Services** — לוגיקה עסקית (`InquiryService`), ולידציה, ושירות קאשינג (`CacheService`).
- **Inquires.Api** — Controller (`InquiriesController`), Middleware לטיפול גלובלי בשגיאות, הרשמת DI ו-Swagger.

הסינון, המיון והעימוד מתבצעים כולם ברמת בסיס הנתונים (IQueryable), ולא בזיכרון. הלקוח (`inquires-client`) הוא אפליקציית Angular עצמאית תחת `inquiries/` (רשימה, סרגל סינון, תג סטטוס, סיכום) שמדברת עם ה-API בלבד דרך `InquiryService`.

## 4. החלטה טכנולוגית משמעותית

**שימוש בטבלאות `Statuses`/`Priorities` נפרדות עם מפתח זר, במקום Enum בקוד.**
הבחירה מאפשרת להוסיף ערך חדש (סטטוס/עדיפות) בהזנת שורה בלבד, ללא deploy של קוד; שומרת על שלמות רפרנציאלית ברמת ה-DB; ומשאירה מקום להרחבה עתידית של כל ערך (למשל צבע או סדר תצוגה) בלי לשנות סכימה. המחיר: יש JOIN נוסף בשאילתות ומיפוי מפורש בין Id לשם בשכבת ה-DTO, אך זה נשאר קריא הודות ל-`InquiryMappingExtensions`.

## 5. מה הייתי משפר/ת עם עוד זמן

מעבר מ-`IMemoryCache` לקאש מבוזר (למשל Redis) — כרגע הקאש חי בזיכרון של המופע היחיד, מה שלא יעבוד נכון בסביבת production עם כמה מופעים של ה-API. הממשק `ICacheService` כבר מופשט כך שהחלפת המימוש לא תדרוש שינוי בקוד הקורא.

## 6. שימוש בכלי AI

נעשה שימוש ב-Claude Code (Anthropic) לאורך הפרויקט: בניית מסמך הארכיטקטורה הראשוני (`ARCHITECTURE.md`), הפקת ה-scaffolding לפרויקטים ולקבצים לפי המבנה שהוגדר, וכתיבת קובץ README זה. עיצוב הארכיטקטורה, ההחלטות המרכזיות והבדיקה הסופית של הקוד נעשו על ידי המפתח/ת.

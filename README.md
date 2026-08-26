# Inquiries — מערכת ניהול פניות

פרויקט בחינה Full Stack: שרת API ב-ASP.NET Core (4 פרויקטים בשכבות) + לקוח Angular, עם SQL Server כבסיס נתונים.

## פונקציונליות עיקרית

- הצגת רשימת פניות
- חיפוש וסינון
- מיון
- עימוד
- עדכון סטטוס
- הצגת סיכומים

## 1. הוראות הרצה

### בסיס נתונים
1. יש SQL Server / LocalDB זמין (מחרוזת החיבור המוגדרת: `(localdb)\mssqllocaldb`, מסד `InquiriesDb`).
2. הרצת הסקריפטים לפי הסדר (למשל דרך SSMS או `sqlcmd`):
   - `Database/Schema.sql` — יוצר את הטבלאות `Statuses`, `Priorities`, `Inquiries` (כולל אינדקסים).
   - `Database/Seed.sql` — מזין כ-10,000 רשומות לדוגמה.

### שרת (API)
```
cd Inquiries.Api
dotnet run
```
ה-API עולה על `http://localhost:5120` (וגם `https://localhost:7245`). ב-Development נגיש Swagger UI מהשורש.

### לקוח (Angular)
```
cd inquiries-client
npm install
npm start
```
האפליקציה עולה על `http://localhost:4200` ומדברת עם ה-API דרך `http://localhost:5120/api` (מוגדר ב-`src/app/core/api-config.ts`).

### בדיקות (Unit Tests)
```
dotnet test Inquiries.Tests
```

## 2. טכנולוגיות בשימוש

- **.NET 10 / ASP.NET Core Web API** — שכבת השרת.
- **Entity Framework Core 10 + SQL Server** — גישה לנתונים, בגישת Database-First.
- **Angular 22 + Angular Material + RxJS** — שכבת הלקוח.
- **IMemoryCache** — קאשינג בזיכרון בשכבת ה-Services.
- **xUnit** — בדיקות יחידה.
- **Swagger / OpenAPI** — תיעוד ובדיקה ידנית של ה-API בסביבת פיתוח.

## 3. מבנה הפתרון

השרת בנוי כ-4 פרויקטים עם כיוון תלות חד-כיווני (ללא מעגליות) — מי מפנה למי:

- `Inquiries.Api` מפנה ל-`Inquiries.Services`
- `Inquiries.Services` מפנה ל-`Inquiries.Data` ול-`Inquiries.DTO`
- `Inquiries.DTO` מפנה ל-`Inquiries.Data`

כלומר `Inquiries.DTO` תלוי רק ב-`Inquiries.Data` (כדי למפות Entity ל-DTO), ואילו `Inquiries.Services` הוא הפרויקט היחיד שתלוי גם ב-`Data` (גישה לנתונים) וגם ב-`DTO` (מודלים של הבקשות/תשובות).

- **Inquiries.Data** — `DbContext`, ישויות (`Inquiry`, `Status`, `Priority`), Repository לגישה לנתונים.
- **Inquiries.DTO** — מודלים של Request/Response ומיפויים בין Entity ל-DTO.
- **Inquiries.Services** — לוגיקה עסקית (`InquiryService`), ולידציה, ושירות קאשינג (`CacheService`).
- **Inquiries.Api** — Controller (`InquiriesController`), Middleware לטיפול גלובלי בשגיאות, הרשמת DI ו-Swagger.

הסינון, המיון והעימוד מתבצעים כולם ברמת בסיס הנתונים (IQueryable), ולא בזיכרון. הלקוח (`inquiries-client`) הוא אפליקציית Angular עצמאית תחת `inquiries/` (רשימה, סרגל סינון, תג סטטוס, סיכום) שמדברת עם ה-API בלבד דרך `InquiryService`.

## 4. החלטה טכנולוגית משמעותית

**שימוש בטבלאות `Statuses`/`Priorities` נפרדות עם מפתח זר, במקום Enum בקוד.**
הבחירה מאפשרת להוסיף ערך חדש (סטטוס/עדיפות) בהזנת שורה בלבד, ללא deploy של קוד; שומרת על שלמות רפרנציאלית ברמת ה-DB; ומשאירה מקום להרחבה עתידית של כל ערך (למשל צבע או סדר תצוגה) בלי לשנות סכימה. המחיר: יש JOIN נוסף בשאילתות ומיפוי מפורש בין Id לשם בשכבת ה-DTO, אך זה נשאר קריא הודות ל-`InquiryMappingExtensions`.

## 5. מה הייתי משפרת עם עוד זמן

טיפול ב-Concurrency באמצעות עמודת `RowVersion` (כפי שהוזכר ב-`ARCHITECTURE.md` כנקודת הרחבה עתידית). כרגע `UpdateStatusAsync` קורא רשומה, משנה אותה ושומר, בלי לבדוק אם מישהו אחר עדכן את אותה פנייה בינתיים — ז"א יש חשיפה ל-lost update. עם `RowVersion` (Optimistic Concurrency ב-EF Core) אפשר לזהות התנגשות כזו ולהחזיר שגיאה מתאימה (409 Conflict) במקום לדרוס בשקט עדכון של משתמש אחר.

## 6. שימוש בכלי AI

התכנון וההחלטות הטכנולוגיות בפרויקט היו שלי לאורך כל הדרך — מבנה השכבות, מודל הנתונים וההחלטות הארכיטקטוניות המתוארות בסעיפים 4-5. נעזרתי בכלי Claude Code (Anthropic) ככלי מסייע לכתיבת הקוד, תוך מעבר, בדיקה ותיקון שוטפים של הקוד שנוצר לאורך כל הפיתוח.

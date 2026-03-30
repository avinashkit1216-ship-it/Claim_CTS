# 🎨 Dark Theme - Quick Reference Guide

## File Organization

```
ClaimSubmission.Web/
├── wwwroot/
│   └── css/
│       ├── dark-theme.css          ← Main theme file (NEW)
│       ├── site.css                ← Global overrides (UPDATED)
│       └── ...
└── Views/
    ├── Shared/
    │   ├── _Layout.cshtml          ← Added CSS link (UPDATED)
    │   ├── _Layout.cshtml.css      ← Updated colors (UPDATED)
    │   └── ...
    ├── Authentication/
    │   └── Login.cshtml            ← Redesigned (UPDATED)
    └── ...
```

---

## CSS Variables Quick Lookup

### Core Colors
```css
:root {
  --bs-body-bg: #0f172a;              /* Page background */
  --bs-body-color: #f1f5f9;           /* Default text color */
  --bs-border-color: #334155;         /* Default border color */
  --bs-card-bg: #1a202c;              /* Card backgrounds */
  --bs-card-border-color: #2d3748;    /* Card borders */
}
```

### Primary Actions
```css
--color-primary: #3b82f6;             /* Blue - main accent */
--color-primary-hover: #2563eb;       /* Blue - darker on hover */
--color-primary-dark: #1e40af;        /* Blue - dark state */
```

### Secondary Actions
```css
--color-secondary: #14b8a6;           /* Teal - secondary accent */
--color-secondary-hover: #0d9488;     /* Teal - darker on hover */
```

### Status Colors
```css
--color-success: #10b981;             /* Green - success/approved */
--color-success-hover: #059669;       /* Green - darker hover */
--color-danger: #ef4444;              /* Red - error/delete */
--color-danger-hover: #dc2626;        /* Red - darker hover */
--color-warning: #f59e0b;             /* Amber - warning/caution */
--color-warning-hover: #d97706;       /* Amber - darker hover */
--color-info: #06b6d4;                /* Cyan - information */
--color-info-hover: #0891b2;          /* Cyan - darker hover */
```

### Text Colors
```css
--color-text-primary: #f1f5f9;        /* Light gray - main text */
--color-text-secondary: #cbd5e1;      /* Medium gray - secondary text */
--color-text-muted: #94a3b8;          /* Dim gray - disabled/muted text */
```

### Background Shades
```css
--color-bg-dark: #0f172a;             /* Darkest background */
--color-bg-darker: #0a0f1a;           /* Very dark */
--color-bg-light: #1a202c;            /* Light background */
--color-bg-lighter: #2d3748;          /* Lighter background */
```

### Transitions
```css
--transition-default: all 0.3s ease;  /* Standard animation */
--transition-fast: all 0.2s ease;     /* Quick animation */
```

---

## Component Classes

### Buttons

```html
<!-- Primary Button (Blue) -->
<button class="btn btn-primary">Primary</button>

<!-- Secondary Button (Teal) -->
<button class="btn btn-secondary">Secondary</button>

<!-- Success Button (Green) -->
<button class="btn btn-success">Success</button>

<!-- Danger Button (Red) -->
<button class="btn btn-danger">Danger</button>

<!-- Warning Button (Amber) -->
<button class="btn btn-warning">Warning</button>

<!-- Info Button (Cyan) -->
<button class="btn btn-info">Info</button>

<!-- Outline Button -->
<button class="btn btn-outline-primary">Outline</button>
```

### Cards

```html
<!-- Basic Card -->
<div class="card">
  <div class="card-header">Title</div>
  <div class="card-body">Content</div>
  <div class="card-footer">Footer</div>
</div>
```

### Forms

```html
<!-- Text Input -->
<input type="text" class="form-control" placeholder="Enter text">

<!-- Select Dropdown -->
<select class="form-select">
  <option>Option 1</option>
</select>

<!-- Checkbox -->
<input type="checkbox" class="form-check-input">
<label class="form-check-label">Checkbox</label>

<!-- Form Group -->
<div class="mb-3">
  <label class="form-label">Label</label>
  <input class="form-control" type="text">
</div>
```

### Tables

```html
<!-- Dark Table -->
<table class="table table-hover table-striped">
  <thead class="table-dark">
    <tr>
      <th>Column 1</th>
      <th>Column 2</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Data 1</td>
      <td>Data 2</td>
    </tr>
  </tbody>
</table>
```

### Alerts

```html
<!-- Success Alert -->
<div class="alert alert-success">Success message</div>

<!-- Danger Alert -->
<div class="alert alert-danger">Error message</div>

<!-- Warning Alert -->
<div class="alert alert-warning">Warning message</div>

<!-- Info Alert -->
<div class="alert alert-info">Information message</div>
```

### Badges

```html
<!-- Primary Badge -->
<span class="badge badge-primary">Primary</span>

<!-- Success Badge -->
<span class="badge badge-success">Approved</span>

<!-- Danger Badge -->
<span class="badge badge-danger">Rejected</span>

<!-- Warning Badge -->
<span class="badge badge-warning">Pending</span>
```

---

## Color Usage Examples

### When to Use Each Color

| Color | Use For | Example |
|-------|---------|---------|
| **Blue** (#3b82f6) | Primary actions, links | Save, Submit, Login buttons |
| **Teal** (#14b8a6) | Secondary actions, hover | Edit, View, Learn more links |
| **Green** (#10b981) | Success, approval | Approved claims, success messages |
| **Red** (#ef4444) | Danger, deletion | Delete, Reject, Error messages |
| **Amber** (#f59e0b) | Warnings, caution | Pending claims, warning alerts |
| **Cyan** (#06b6d4) | Information, tips | Info messages, help text |

---

## Transition Timing

```css
/* Standard transitions (most elements) */
transition: all 0.3s ease;

/* Fast transitions (interactive) */
transition: all 0.2s ease;

/* Specific property transitions */
transition: background-color 0.3s ease;
transition: color 0.2s ease;
transition: transform 0.3s ease;
```

---

## Responsive Breakpoints

```css
/* Mobile (< 576px) */
@media (max-width: 576px) {
  /* Mobile styles */
}

/* Tablet (576px - 768px) */
@media (max-width: 768px) {
  /* Tablet styles */
}

/* Desktop (> 768px) */
@media (min-width: 768px) {
  /* Desktop styles */
}
```

---

## Creating Custom Components

### Using CSS Variables
```css
.custom-component {
  background-color: var(--color-bg-light);
  color: var(--color-text-primary);
  border: 1px solid var(--color-bg-lighter);
  border-radius: 8px;
  padding: 1rem;
  transition: var(--transition-default);
}

.custom-component:hover {
  background-color: var(--color-bg-lighter);
  color: var(--color-primary);
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.2);
}
```

### Box Shadow Utility Classes
```css
.box-shadow { box-shadow: 0 0.25rem 0.75rem rgba(0, 0, 0, 0.1); }
.box-shadow-sm { box-shadow: 0 0.125rem 0.25rem rgba(0, 0, 0, 0.2); }
.box-shadow-lg { box-shadow: 0 1rem 3rem rgba(59, 130, 246, 0.15); }
```

### Text Utility Classes
```html
<p class="text-primary">Primary text</p>
<p class="text-secondary">Secondary text</p>
<p class="text-muted">Muted text</p>
<p class="text-success">Success text</p>
<p class="text-danger">Danger text</p>
```

---

## Common Patterns

### Button Hover Effect
```css
.btn {
  transition: all 0.3s ease;
}

.btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 16px rgba(59, 130, 246, 0.5);
}
```

### Form Input Focus
```css
.form-control:focus {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 0.25rem rgba(59, 130, 246, 0.25);
}
```

### Card Hover Effect
```css
.card:hover {
  transform: translateY(-2px);
  border-color: var(--color-primary);
  box-shadow: 0 12px 24px rgba(59, 130, 246, 0.15);
}
```

### Link Hover Animation
```css
a {
  color: var(--color-primary);
  transition: color 0.2s ease;
}

a:hover {
  color: var(--color-secondary);
  text-decoration: underline;
}
```

---

## Accessibility Features

### Focus Visible
```css
*:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
}
```

### High Contrast
All text meets WCAG AA contrast standards (4.5:1 minimum)

### Keyboard Navigation
All interactive elements support keyboard navigation

---

## Browser DevTools Tips

### Inspect Colors
1. Right-click element → Inspect
2. Look for `--color-*` variables in :root
3. Change colors in DevTools to test

### Edit Transitions
1. Open DevTools
2. Modify `--transition-default` value
3. Watch animations update in real-time

### Test Responsive
1. Press Ctrl+Shift+M (DevTools responsive mode)
2. Select different device sizes
3. Verify layout adapts

---

## File Size Reference

```
dark-theme.css      ~50KB   (full)
dark-theme.min.css  ~20KB   (minified recommended for production)
site.css            ~2KB    (global overrides)
bootstrap.css       ~150KB  (unchanged)

Total CSS payload:  ~200KB  (acceptable for modern web)
```

---

## Animation Classes

```html
<!-- Fade in animation -->
<div class="fade-in">Content</div>

<!-- Slide in animation -->
<div class="slide-in">Content</div>
```

---

## Import in Custom CSS

```css
/* Import to use variables in other files */
@import url('dark-theme.css');

.my-component {
  background-color: var(--color-bg-light);
  color: var(--color-text-primary);
}
```

---

## Debugging Tips

### Check if CSS is loaded
```javascript
// In browser console:
window.getComputedStyle(document.body).getPropertyValue('--color-primary')
// Should return: #3b82f6
```

### Verify colors
```javascript
// Get all component colors:
const style = getComputedStyle(document.documentElement);
console.log(style.getPropertyValue('--color-primary'));
console.log(style.getPropertyValue('--color-success'));
```

### Test animations
```javascript
// Disable animations temporarily:
document.body.style.transition = 'none !important';
```

---

## Rollback Instructions

### If you need to revert to original theme:
1. Remove dark-theme.css link from _Layout.cshtml
2. Restore original site.css (or undo changes)
3. Restore original _Layout.cshtml.css
4. Rebuild the project

---

## Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Colors not updating | Clear browser cache (Ctrl+Shift+Delete) |
| Animations lag | Disable unnecessary transitions |
| Text hard to read | Check zoom level, adjust contrast |
| Mobile looks broken | View in responsive mode, check breakpoints |
| Buttons not clickable | Check z-index, ensure elements not overlapping |

---

## Performance Optimization

### For Production
1. Minify dark-theme.css
2. Combine with site.css if possible
3. Enable gzip compression
4. Use CDN for assets
5. Consider critical CSS inlining

### CSS Minification
```bash
# Using popular tools
npx cleancss dark-theme.css -o dark-theme.min.css
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-03-30 | Initial dark theme implementation |

---

## Support & Questions

For questions about specific components or customization, refer to:
- **Technical Details:** `UI_ENHANCEMENT.md`
- **Implementation Summary:** `DARK_THEME_IMPLEMENTATION_SUMMARY.md`
- **CSS File:** `wwwroot/css/dark-theme.css` (well-commented)

---

**Last Updated:** March 30, 2026  
**Status:** ✅ Complete

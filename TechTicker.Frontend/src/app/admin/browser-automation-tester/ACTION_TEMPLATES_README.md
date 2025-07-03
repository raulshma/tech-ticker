# Action Templates Feature

## Overview

The Action Templates feature allows users to save and reuse browser automation action sequences in the Browser Automation Profile Tester. This feature provides a comprehensive template management system with local storage persistence.

## Features

### 1. Template Management
- **Create Templates**: Save current action sequences as reusable templates
- **Edit Templates**: Modify existing templates with a full-featured editor
- **Delete Templates**: Remove templates you no longer need
- **Duplicate Templates**: Create copies of existing templates
- **Search & Filter**: Find templates by name, description, or category

### 2. Template Categories
Templates are organized into categories for easy management:
- **Navigation**: Basic page navigation and waiting
- **Form Filling**: Form input and submission actions
- **Clicking**: Element clicking and interaction
- **Waiting**: Various wait and timeout actions
- **Scrolling**: Page scrolling actions
- **Screenshots**: Screenshot capture sequences
- **Custom**: User-defined custom actions
- **E-commerce**: Shopping and product browsing
- **Social Media**: Social media interaction patterns
- **Testing**: Testing and validation sequences

### 3. Action Types Supported
The system supports all browser automation action types:

**Navigation Actions:**
- `navigate` / `goto` / `url` - Navigate to a URL
- `reload` / `refresh` - Reload the current page
- `goback` - Navigate back in browser history
- `goforward` - Navigate forward in browser history

**Clicking Actions:**
- `click` - Click on an element by CSS selector
- `doubleclick` - Double-click on an element
- `rightclick` - Right-click on an element

**Input Actions:**
- `type` - Type text into an input field
- `clear` - Clear an input field
- `setValue` - Set input value using JavaScript
- `press` - Press a keyboard key
- `upload` - Upload a file to a file input

**Focus Actions:**
- `focus` - Focus on an element
- `blur` - Remove focus from an element
- `hover` - Hover over an element

**Wait Actions:**
- `wait` / `waitForTimeout` - Wait for specified time
- `waitForSelector` - Wait for an element to appear
- `waitForNavigation` - Wait for page navigation to complete
- `waitForLoadState` - Wait for specific page load state

**Scroll Actions:**
- `scroll` - Scroll down by one viewport

**Selection Actions:**
- `selectOption` - Select option from dropdown

**Media Actions:**
- `screenshot` - Capture screenshot

**JavaScript Actions:**
- `evaluate` - Execute custom JavaScript code

**Drag & Drop Actions:**
- `drag` - Drag and drop elements

**Window Management:**
- `maximize` - Maximize browser window
- `minimize` - Minimize browser window
- `fullscreen` - Enter fullscreen mode
- `newtab` / `newpage` - Open new tab or page
- `closetab` / `closepage` - Close current tab or page
- `switchwindow` / `switchtab` - Switch between windows or tabs

**Frame Actions:**
- `switchframe` / `switchiframe` - Switch to a frame or iframe

**Alert Actions:**
- `alert` - Handle alert dialogs
- `acceptalert` - Accept an alert dialog
- `dismissalert` - Dismiss an alert dialog

**Cookie Actions:**
- `getcookies` - Retrieve cookies
- `setcookies` - Set cookies
- `deletecookies` - Delete cookies

**Style & Script Injection:**
- `addstylesheet` - Add CSS stylesheet to page
- `addscript` - Add JavaScript to page

**Device Emulation:**
- `emulatedevice` - Emulate specific device settings

### 4. Template Properties
Each template includes:
- **Name**: Descriptive template name
- **Description**: Optional description of what the template does
- **Category**: Template category for organization
- **Actions**: Array of browser automation actions
- **Created Date**: When the template was created
- **Last Used**: When the template was last used
- **Usage Count**: How many times the template has been used

## Usage

### Accessing Action Templates
1. Navigate to the Browser Automation Tester
2. Click on the "Action Templates" tab
3. Use the "Manage Templates" button to open the template dialog

### Creating a Template
1. **From Current Actions**:
   - Add actions to your current sequence
   - Click "Save Current Actions" button
   - Fill in template details and save

2. **From Template Dialog**:
   - Click "Create Template" button
   - Add actions using the action builder
   - Fill in template details and save

### Using a Template
1. Open the Action Templates dialog
2. Browse or search for the desired template
3. Click "Use Template" to apply it to your current actions
4. The template actions will replace your current actions

### Managing Templates
- **Edit**: Click the menu button on any template and select "Edit"
- **Duplicate**: Create a copy of an existing template
- **Delete**: Remove templates you no longer need
- **Search**: Use the search field to find specific templates
- **Filter**: Use the category dropdown to filter templates

## Default Templates

The system includes several pre-built templates to get you started:

### Basic Navigation
- Navigate to a URL with a wait period
- Perfect for simple page loading tests

### Form Filling
- Wait for form elements
- Fill in username and password fields
- Submit the form
- Ideal for login automation

### Screenshot Sequence
- Navigate to a page
- Take screenshots at different points
- Scroll and capture additional screenshots
- Great for visual testing

### E-commerce Browse
- Navigate to a product page
- Wait for product grid to load
- Click on first product
- Wait for product details
- Take a screenshot
- Perfect for e-commerce testing

## Technical Details

### Storage
- Templates are stored in browser localStorage
- Data persists between browser sessions
- Templates are user-specific (not shared between users)

### Data Structure
```typescript
interface ActionTemplate {
  id: string;
  name: string;
  description?: string;
  category: string;
  actions: BrowserAutomationAction[];
  createdAt: Date;
  lastUsed?: Date;
  usageCount: number;
}

interface BrowserAutomationAction {
  actionType: string;
  selector?: string;
  repeat?: number;
  delayMs?: number;
  value?: string;
}
```

### Integration
- Templates integrate seamlessly with the existing browser automation system
- Actions from templates are converted to DTOs when starting tests
- Template usage statistics are tracked automatically

## Best Practices

### Creating Effective Templates
1. **Use Descriptive Names**: Make template names clear and specific
2. **Add Descriptions**: Explain what the template does and when to use it
3. **Choose Appropriate Categories**: Help others find your templates
4. **Test Your Templates**: Verify templates work before sharing
5. **Keep Templates Focused**: Create templates for specific use cases

### Template Organization
1. **Use Categories**: Organize templates by purpose
2. **Regular Cleanup**: Remove outdated or unused templates
3. **Version Control**: Use descriptive names for different versions
4. **Documentation**: Add descriptions to explain complex templates

### Performance Considerations
1. **Limit Action Count**: Keep templates under 20 actions for best performance
2. **Use Appropriate Delays**: Include sufficient delays between actions
3. **Optimize Selectors**: Use efficient CSS selectors
4. **Test on Target Sites**: Verify templates work on actual target websites

## Troubleshooting

### Common Issues
1. **Template Not Loading**: Check if localStorage is enabled in your browser
2. **Actions Not Working**: Verify selectors are correct for your target site
3. **Performance Issues**: Reduce the number of actions or increase delays
4. **Template Not Saving**: Ensure you have sufficient localStorage space

### Getting Help
- Check the browser console for error messages
- Verify that all required fields are filled when creating templates
- Test templates on simple pages before using on complex sites
- Use the browser's developer tools to debug selector issues

## Future Enhancements

Planned improvements for the Action Templates feature:
- **Template Sharing**: Share templates between users
- **Template Import/Export**: Import and export templates as JSON files
- **Template Versioning**: Track template versions and changes
- **Advanced Search**: More sophisticated search and filtering options
- **Template Analytics**: Detailed usage analytics and insights
- **Cloud Storage**: Store templates in the cloud for cross-device access
- **Template Marketplace**: Browse and download community-created templates 
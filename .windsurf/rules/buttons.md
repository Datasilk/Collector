---
trigger: always_on
description: when creating/modifying buttons in React
---

* buttons already have CSS styling, so don't style them
* only use classNames for button elements that are provided by these rules:
    * use icon class for buttons that don't have text but use an Icon component instead
    * use cancel class for buttons that have the text "Cancel"
* buttons use a solid background color with white text
* buttons placed inside of a container with the className="tool-bar" have a transparent background with colored text, and when hovered over, they have a colored border
* buttons in a tool-bar container should be wrapped in a div using className="right-side" if the tool-bar is used at the top of the page for page header or section header
* buttons for popup modals (okay, confirm, cancel, etc) should not be inside a tool-bar container but instead, inside a container with className="buttons"
* don't ever put an inline function on a button event. Use "handle" functions instead
* because the timer is outside of the current state, make the timer pass the input value into the function that the timer will call so that the function can use the passed parameter (if passed) or fall back to the state object if no parameter is passed
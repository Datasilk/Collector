---
trigger: always_on
description: when adding debounce functionality to an input field in React web app
---

* only Input components are allowed to debounce, and they should only debounce if they call an API when during onInput
* add a stateful object for tracking the debounce timer
* only debounce within Input onInput handler function, don't debounce within useEffect
* make sure to clear debounce timer state before setting the state of the new timer
* don't debounce updating the state of the input value, instead, debounce the execution of the api associated with the input field
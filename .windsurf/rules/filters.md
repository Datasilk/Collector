---
trigger: always_on
---

* filter system in react page should use a filter stateful object that contains all parameters that will be used in the filter. 
* any debounced inputs should pass a new object inherited from the filter stateful object into the method that the timer will call
* all filters fields must be inside a div.filter element
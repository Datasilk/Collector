---
trigger: model_decision
description: when working with pagination
---

* please use this HTML structure for pagination:

<div className="tool-bar center pagination">
    <button
        onClick={() => handlePageChange(-1)}
        disabled={filterOptions.start === 0}
        aria-label="Previous"
        className={filterOptions.start === 0 ? "disabled" : ""}
    ><Icon name="chevron_left" /></button>
    <span className="page-info">{filterOptions.start / filterOptions.length + 1} / {totalPages}</span>
    <button
        onClick={() => handlePageChange(1)}
        disabled={(filterOptions.start / filterOptions.length) + 1 >= totalPages}
        aria-label="Next"
        className={(filterOptions.start / filterOptions.length) + 1 >= totalPages ? "disabled" : ""}
    ><Icon name="chevron_right" /></button>
</div>
```json
{
  "type": "object",
  "properties": {
    "page_title": { "type": "string" },
    "author": { "type": ["string", "null"] },
    "publish_date": { "type": ["string", "null"] },
    "hero_image": {
      "type": ["object", "null"],
      "properties": {
        "url": { "type": "string" },
        "alt_text": { "type": "string" },
        "caption": { "type": ["string", "null"] }
      }
    },
    "summary": { "type": "string" },
    "sections": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "heading": { "type": ["string", "null"] },
          "blocks": {
            "type": "array",
            "items": {
              "type": "object",
              "properties": {
                "type": { "type": "string", "enum": ["paragraph", "bullet_list", "numbered_list"] },
                "text": { "type": ["string", "null"] },
                "items": { "type": "array", "items": { "type": "string" } }
              }
            }
          },
          "images": {
            "type": "array",
            "items": {
              "type": "object",
              "properties": {
                "url": { "type": "string" },
                "alt_text": { "type": "string" },
                "caption": { "type": ["string", "null"] }
              }
            }
          },
          "tables": {
            "type": "array",
            "items": {
              "type": "object",
              "properties": {
                "caption": { "type": ["string", "null"] },
                "headers": { "type": "array", "items": { "type": "string" } },
                "rows": {
                  "type": "array",
                  "items": {
                    "type": "array",
                    "items": { "type": "string" }
                  }
                }
              }
            }
          }
        }
      }
    },
    "links": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "text": { "type": "string" },
          "url": { "type": "string" }
        }
      }
    },
    "comments": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "username": { "type": "string" },
          "comment": { "type": "string" },
          "replies": {
            "type": "array",
            "items": {
              "type": "object",
              "properties": {
                "username": { "type": "string" },
                "comment": { "type": "string" }
              }
            }
          }
        }
      }
    },
    "search_engine_submit_form": {
      "type": "object",
      "description": "If the page offers a site-search facility (a search box that submits a query to this website), report it here; otherwise omit this field.",
      "properties": {
        "url": { "type": "string" }
      }
    }
  }
}

```
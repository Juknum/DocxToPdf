---
name: python-scratch-execution
description: Directives for executing Python scripts safely via scratch files and mandatory initial conversation skill checks.
---

# Python Scratch Execution Directive

## Rules:
1. **No Inline Python Command Execution**:
   - NEVER execute inline python scripts using `python3 -c "..."` from the command line.
   - ALWAYS create a `.py` script in the scratch directory (`<appDataDir>/brain/<conversation-id>/scratch/`) using `write_to_file`.
   - Execute the script using `python3 <appDataDir>/brain/<conversation-id>/scratch/<script-name>.py`.

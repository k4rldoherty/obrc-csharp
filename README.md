# 1 Billion Row Challenge (C# Edition)

This repository contains my personal attempt at the **1 Billion Row Challenge (1BRC)**. The goal is to parse a text file containing 1,000,000,000 temperature measurements and calculate the min, max, and mean temperature per station as quickly as possible.

## The Problem
The challenge is to process a ~10GB file with entries formatted as `<string: station name>;<double: measurement>`. 
As an extra challenge, I decided to also write my own 1 billion row data generator, and optimize that as well.

**Example:**
`Hamburg;12.0`  
`Bulawayo;8.9`  
`Palermo;14.3`
---

## Rules and limits

- No external library dependencies may be used. That means no lodash, no numpy, no Boost, no nothing. You're limited to the standard library of your language.
- Implementations must be provided as a single source file. Try to keep it relatively short; don't copy-paste a library into your solution as a cheat.
- The computation must happen at application runtime; you cannot process the measurements file at build time
- Input value ranges are as follows:
  - Station name: non null UTF-8 string of min length 1 character and max length 100 bytes (i.e. this could be 100 one-byte characters, or 50 two-byte characters, etc.)
  - Temperature value: non null double between -99.9 (inclusive) and 99.9 (inclusive), always with one fractional digit
  - There is a maximum of 10,000 unique station names.
  - Implementations must not rely on specifics of a given data set. Any valid station name as per the constraints above and any data distribution (number of measurements per station) must be supported.

---

## Output 

- The program should print out the min, mean, and max values per station, alphabetically ordered.

**Example:**
Hamburg;12.0;23.1;34.2
Bulawayo;8.9;22.1;35.2
Palembang;38.8;39.9;41.0

---

## Tech & Optimization Strategy
To squeeze every ounce of performance out of .NET, this implementation explores:
- Multi threaded
- Channels
- Batch inserts
- Shared array pool

---

## Performance Benchmarks

### Input Generator

| Version | Description | Execution Time (s) |
| :--- | :--- | :--- |
| **v1.0** | Base Implementation | 391 |
| **v2.0** | Single threaded, batch inserts, more efficient | 343 |
| **v3.0** | Multi threaded, 7 worker threads and 1 writer thread | 150 |
| **v4.0** | ?? | ?? |

### Input Parser
| Version | Description | Execution Time (ms) |
| :--- | :--- | :--- |
| **v1.0** | Base Implementation | -- |
| **v2.0** | -- | -- |
| **v3.0** | -- | -- |

---

## Resources
* [Official 1BRC Repository](https://github.com/gunnarmorling/1brc)

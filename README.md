# Introduction
The aim of this project is to create a open-source, general-purpose training platform, where teachers, parents and researchers
can add or modify exercises and training paradigms.

## Stimulus/response pairs
The core of the system is the handling of stimulus-response pairs.
The system decides what stimulus to display - based on algorithms, settings and previous responses - and registers/analyzes the response.
A stimulus can be a wide variety of things, e.g.
* a sequence of positions to remember
* a math or logical problem to solve
* a multiple-choice question
* a prompt to explain a concept
* a musical phrase to play back on a piano keyboard

Currently, the code in this repository handles the data produced of a such an interactive "training client" system (storing results, aggregating into statistics), and provides a user interface to view this data.

Work to implement a training client has begun, but is still in an early stage.

# Why this project started
There are many products in the e-learning space, but what I've seen so far they fall short of my ideal:
* Little focus on interactive parts of the training (moreso on "content" as text or videos)
* Limited or inexistent possibilities for users to
  * modify algorithms
  * create or modify skins
  * provide bug fixes
  * share modifications with others
* No way to self-host the system
* Limited support for creating new training clients

This project aims to provide an open-source platform that makes it easy to contribute with modifications, as well as a hosted service where users can access the system for free or at cost price.

Some scenarios that this system aims to support
### Researchers
* Using available tests and exercises to evaluate control vs active groups in interventions otherwise unrelated to the system - e.g. does additional PE lessons improve logical reasoning or reaction times
* Evaluate new training paradigms - a large set of subjects from the general user population can potentially be included (given approvals from end users and system/school administrators)

### Teachers
While in a majority of cases, it's probably best to follow the established training protocols, 
teachers might still want to add or change parts of the system
* Add problems to existing sets (e.g. math problems, vocabularies)
* Modify algorithms for level adaption (adjust for needs of failure-adverse students)
* Add exercises for domains that are currently not covered

### Students
* A class of 9th-graders creates custom graphics / sounds for their school
* As part of computer class, students develop small exercises / games for their younger peers to use

# Modifying the system
As long as modifications to the system are made publicly available, users are allowed to deploy their modified copy to their own infrastructure. However, it's in the project's interest to incentivize usage of the common infrastructure:
* Training results can be analyzed in comparison to larger groups, providing insights into the training effects of the modification
* Improvements and modifications can be shared more easily
* Modifications can be vetted by the community, both for code quality and for identifying security concerns or harmful content
  

Ways to modify functionality
* Pull requests to the main repository
  * Ensures quality by the peer review process
  * Makes functionality available to all users
* Plugins loaded at runtime
  * Faster development
  * For local use only (since we cannot guarantee security/quality)
* Edit configurations (training plans)
* Create a completely new training client

Examples of modifications
* re-skinning the training client (create completely new themes)
* change presentation of an exercise
* improve the algorithm for generating working memory sequences
* add speech recognition as an alternative input method 


# System components

* Backend
  * Authentication
  * Training settings, user-generated data, aggregated data
  * Organizational structure (relations between student/teacher/parent/school/headmaster etc)
* Admin frontend
  * Teacher / class views
    * Create trainings and modify settings
    * View results/progress on class or student level
    * Get help identifying and supporting low-performing students
    * Monitor class activity in real time, to quickly provide help if someone's stuck or responding randomly
* Training client
  * Logic - level management, stimulus generation, response analysis
    * The reference implementation will probably reside on a server
      * Frontend application requests a stimulus and sends back the users response
      * Potentially make available as a WASM package for offline use
  * GUI - presentation and interaction handling
    * First implementation will be a simple console application
    * A version in HTML/JS is likely the most accessible alternative going forward
    * Godot / Unity clients might be useful for e.g. VR scenarios

# Current state
The codebase is fully functional as a server for handling training data and providing a front-end for teachers. 
However, this project has been a bit of a playground to explore new ideas and technologies, some of which were dead ends and should eventually be replaced.


# License
TBD: most likely GPLv3 or AGPLv3
You are free to modify and deploy your own version of this software.
However, due to the copy-left license, any modifications made to the code must also be published as open source.
If the code is used as a service, then the codebase calling into that service must also be published as open source (AGPLv3).

Licensing under GPLv3 means we can use many libraries and services for free, e.g. [Neo4j Community Edition](https://neo4j.com/pricing/).


Needs [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio) for local dev

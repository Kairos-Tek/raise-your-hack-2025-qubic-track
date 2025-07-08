![Qbuild logo](qbuild-logo.png)

## What is Qbild

Qbuild is a fully functional platform that allows you to quickly and easily audit any Qubic smart contract, identifying potential vulnerabilities and generating automatically a comprehensive audit report.

But that's just the beginning!

Qbuild not only process automatically your smart contract to identify vulnerabilities, categorize them by severity, how they could be exploited and suggest recommendations to avoid them, generating a complete report.

The platform deploys the smart contract in a controlled Qubic testing environment, building automatically a complete battery of tests to exploit each vulnerability, calling the identified smart contract procedures and/or functions with specific payload or arguments, allowing developers to verify the real impact of each potential vulnerability without the need to build any specific test tool.
Just fasten your seatbelt, keep your seat upright, and press the button to see what happens!

Learn more about Qbuild and test your smart contracts (if you dare...) at the [Qbuild website built in Raise Your Hack](https://qbuild-pre.kairos-tek.com/).

*Disclaimer: depending the vulnerabilities, even Qubic test node could hang… believe us, we know how to do it!*

## Qbuild technologies

QBuild integrates a wide range of technologies to provide a seamless and powerful development experience for Qubic smart contracts.

At the core of our system is a dedicated Qubic test node, where contracts are deployed and executed in a fully controlled environment. We’ve leveraged the Qubic TypeScript library, combined with reverse engineering of QForge's frontend, to implement key functionalities such as broadcasting transactions and querying smart contracts. Additionally, we make extensive use of the Qubic CLI and connect directly to the Qubic RPC interface to interact with the network at a low level.

For vulnerability analysis and audit generation, QBuild uses advanced large language models, including LLaMA 3.3 70B Versatile via the Groq API, to identify issues and generate detailed, actionable audit reports.

On the frontend, we built the platform using Angular, enhanced by the Vristo UI kit and styled with Tailwind CSS to create a clean and intuitive interface.

The backend is built on .NET 8.0, and the full infrastructure is hosted on AWS:
– AWS Lambda and API Gateway power the backend,
– The frontend is served via S3 and CloudFront,
– And data persistence is handled through a PostgreSQL database.

QBuild is a fully functional and scalable platform, built with modern tools to meet the needs of developers building on Qubic.

## Repository structure

This repository contains three main projects:

- `frontend/`: The frontend application built with Angular.
- `backend/`: The backend API built with .NET 8 (ASP.NET Core).
- `smart-contract/`: The Qubic HM25.h smart contract built specifically to demonstrate Qbuild

```
/
├── frontend/          # Angular application
├── backend/           # .NET 8 Web API
├── smart-contract/    # Qubic smart contract
├── .gitignore
└── README.md
└── qbuild-logo.png
```

## Kairos team at Rise Your Hack

[Kairos](https://www.kairos-tek.com/), the award-winning team at the [first Qubic hackathon](https://x.com/MillyCrypto_/status/1903936236093403637) with [Easy Connect](https://www.kairos-tek.com/easyconnect), currently part of the [Qubic ecosystem](https://qubic.org/ecosystem/easy-connect), has developed Qbuild, the ultimate solution for facilitating the secure development of smart contracts on this network.

- **Jorge Ordovás (Product and Business Development / Marketing and Communications):**
  - Information Technology professional with 25 years of experience in the development of products and services in many different sectors (telecommunications, payments, security, eHealth, energy, cloud, blockchain, web3).
  - Working in Blockchain consulting and development of projects based on blockchain technologies since 2015 when he cofounded [Nevtrace](https://nevtrace.com), the first Blockchain lab in Spain.
  - Senior manager working in Blockchain and Web3 product and business development since 2018 at [Telefonica](https://metaverso.telefonica.com/en/welcome-to-metaverse).
  - **LinkedIn (+10,000 followers):** [Jorge Ordovas](https://www.linkedin.com/in/jorgeordovas/)
  - **X (+6,000 followers):** [@joobid](https://x.com/joobid) and [@nevtrace](https://x.com/nevtrace)
  - **Farcaster (+3,000 followers):** [@joobid.eth](https://warpcast.com/joobid.eth) and [@nevtrace](https://warpcast.com/nevtrace)

- **Rafael Montaño (UX/UI and Product Design)**
  - Co-founder of [Loiband](https://www.loiband.com/home-en), technology consultancy and UX Product Specialist, with experience in UX/UI and product design. He has led the creation of more than 10 scalable digital products.
  - His approach combines user research, prototyping, and design systems in apps, Web2, and Web3, helping startups and companies launch innovative and optimized solutions for the digital market.
  - **LinkedIn:** [Rafael Montaño](https://www.linkedin.com/in/rafael-monta%C3%B1o-marroquin/)
 
- **Jesús Lanzarote (Full Stack Development)**
  - Co-founder and CTO of Loiband. He has been programming for over 25 years and has worked in the startup world for 15 years, where he has participated in all types of projects across multiple areas (eHealth, services, insurance, legal, etc.), leading teams and directly participating in design and development.
  - His current focus is on developing AI-based and Web3 projects.
  - **LinkedIn:** [Jesús Lanzarote](https://www.linkedin.com/in/jesus-lanzarote/)

- **Max García (Full Stack Development)**
   - Enthusiastic programmer with over 7 years of experience in web development. Committed to developing excellent solutions. 
   - **LinkedIn:** [Max García](https://www.linkedin.com/in/maximiliano-daniel-garc%C3%ADa-716705229/)
     
 - **Alberto García (Blockchain RSE)**
   - Computer Engineer from Universidad Carlos III de Madrid and holder of a Master's Degree in Blockchain, Smart Contracts, and CryptoEconomics from Universidad de Alcalá. He brings over eight years of hands-on experience in Business Intelligence and Big Data, and more than seven years in Blockchain technology, working across sectors such as Banking, Retail, Insurance, and Telecommunications. He worked for 7 years a Technical Specialist and Product Manager at Telefónica’s Blockchain team.
   - **LinkedIn:** [Alberto García](https://www.linkedin.com/in/okalberto/)

## Contact

Contact us for more information and become an early user when Qbuild is available!:

- [Qbuild X account](https://x.com/_qbuild_)
- [Kairos X account](https://x.com/kairos_tek)
- [qbuild@kairos-tek.com](mailto:qbuild@kairos-tek.com)
- [Kairos wesite](https://kairos-tek.com)

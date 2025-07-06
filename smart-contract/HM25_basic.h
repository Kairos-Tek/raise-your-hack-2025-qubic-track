using namespace QPI;

struct HM252
{
};

struct HM25 : public ContractBase
{
public:
    struct proc01_input{};
    struct proc01_output{};
    
    struct proc02_input{};
    struct proc02_output{};
    
    struct proc03_input{};
    struct proc03_output{};
    
    struct proc04_input{};
    struct proc04_output{};
    
    struct proc05_input{};
    struct proc05_output{};

    struct func01_input{};
    struct func01_output
    {
        uint64 var1;
    };
    
    struct func02_input{};
    struct func02_output
    {
        uint64 var2;
    };

    struct func03_input{};
    struct func03_output
    {
        uint64 var3;
    };

    struct func04_input{};
    struct func04_output
    {
        uint64 var4;
    };

    struct func05_input{};
    struct func05_output
    {
        uint64 var5;
    };

private:
    uint64 var0;
    uint64 var1;
    uint64 var2;
    uint64 var3;
    uint64 var4;
    uint64 var5;

    PUBLIC_PROCEDURE(proc01) // Overflow risk
        state.var1 = state.var1 * state.var2 * state.var3 * state.var4 * state.var5;
   _

    PUBLIC_PROCEDURE(proc02) // Infinite loop until overflow
        while (state.var2>0)
            state.var2 = state.var2 * 1000000;
   _

    PUBLIC_PROCEDURE(proc03) // Divide by zero
        state.var3 = state.var1 / state.var0;
   _

    PUBLIC_PROCEDURE(proc04) // Infinite call between proc04 and proc05
        state.var4++;
        // proc05();
   _

    PUBLIC_PROCEDURE(proc05)
        state.var5++;
        // proc04();
   _

    PUBLIC_FUNCTION(func01)  // Overflow
        output.var1 = state.var2 + state.var3 + state.var4 + state.var5;
    _

    PUBLIC_FUNCTION(func02)  // Infinite loop until overflow
        while (state.var2>0)
            output.var2 += state.var2 * 1000000;
    _

    PUBLIC_FUNCTION(func03) // Divide by zero
        output.var3 = state.var3 / state.var0;
    _

    PUBLIC_FUNCTION(func04) // Infinite call between func04 and func05
        output.var4++;
        //return func05();
    _

    PUBLIC_FUNCTION(func05)
        output.var5++;
        //return func04();
    _

    REGISTER_USER_FUNCTIONS_AND_PROCEDURES

        REGISTER_USER_PROCEDURE(proc01, 1);
        REGISTER_USER_PROCEDURE(proc02, 2);
        REGISTER_USER_PROCEDURE(proc03, 3);
        REGISTER_USER_PROCEDURE(proc04, 4);
        REGISTER_USER_PROCEDURE(proc05, 5);

        REGISTER_USER_FUNCTION(func01, 1);
        REGISTER_USER_FUNCTION(func02, 2);
        REGISTER_USER_FUNCTION(func03, 3);
        REGISTER_USER_FUNCTION(func04, 4);
        REGISTER_USER_FUNCTION(func05, 5);
    _

    INITIALIZE
        state.var0 = 0;
        state.var1 = 1;
        state.var2 = 2;
        state.var3 = 3;
        state.var4 = 4;
        state.var5 = 5;
    _
};

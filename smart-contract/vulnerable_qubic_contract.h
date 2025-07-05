using namespace QPI;

struct TestBank : public ContractBase
{
public:
    struct Deposit_input
    {
        uint64 amount;
    };
    struct Deposit_output
    {
        uint64 newBalance;
    };

    struct Withdraw_input
    {
        uint64 amount;
    };
    struct Withdraw_output
    {
        uint64 remainingBalance;
    };

    struct Transfer_input
    {
        id recipient;
        uint64 amount;
    };
    struct Transfer_output
    {
        uint64 senderBalance;
        uint64 recipientBalance;
    };

    struct GetBalance_input
    {
        id user;
    };
    struct GetBalance_output
    {
        uint64 balance;
    };

    struct AdminWithdraw_input
    {
        id user;
        uint64 amount;
    };
    struct AdminWithdraw_output
    {
        uint64 withdrawnAmount;
    };

private:
    struct UserBalance
    {
        id user;
        uint64 balance;
    };
    
    Collection<UserBalance, 1000000> userBalances;
    id admin;
    uint64 totalDeposits;
    bool reentrancyLock;

    PUBLIC_PROCEDURE(Deposit)
        // VULNERABILITY 1: No invocationReward validation
        auto reward = qpi.invocationReward();
        
        // VULNERABILITY 2: Integer overflow potential
        auto& userBalance = getUserBalance(qpi.invocator());
        userBalance.balance += input.amount;
        totalDeposits += input.amount;
        
        output.newBalance = userBalance.balance;
    _

    PUBLIC_PROCEDURE(Withdraw)
        // VULNERABILITY 3: Reentrancy - no protection
        auto& userBalance = getUserBalance(qpi.invocator());
        
        if (userBalance.balance >= input.amount)
        {
            // VULNERABILITY 4: State change after external call
            qpi.transfer(qpi.invocator(), input.amount);
            userBalance.balance -= input.amount;
        }
        
        output.remainingBalance = userBalance.balance;
    _

    PUBLIC_PROCEDURE(Transfer)
        auto& senderBalance = getUserBalance(qpi.invocator());
        auto& recipientBalance = getUserBalance(input.recipient);
        
        // VULNERABILITY 5: No balance validation
        senderBalance.balance -= input.amount;
        
        // VULNERABILITY 6: Integer overflow on recipient
        recipientBalance.balance += input.amount;
        
        output.senderBalance = senderBalance.balance;
        output.recipientBalance = recipientBalance.balance;
    _

    PUBLIC_FUNCTION(GetBalance)
        auto& userBalance = getUserBalance(input.user);
        output.balance = userBalance.balance;
    _

    PUBLIC_PROCEDURE(AdminWithdraw)
        // VULNERABILITY 7: Weak access control
        if (qpi.invocator().u64._1 == admin.u64._1)
        {
            auto& userBalance = getUserBalance(input.user);
            
            // VULNERABILITY 8: No balance check
            qpi.transfer(qpi.invocator(), input.amount);
            userBalance.balance -= input.amount;
            
            output.withdrawnAmount = input.amount;
        }
    _

private:
    UserBalance& getUserBalance(id user)
    {
        // VULNERABILITY 9: Inefficient search, potential DoS
        auto index = userBalances.headIndex(user);
        while (index != NULL_INDEX)
        {
            auto& balance = userBalances.element(index);
            if (balance.user == user)
            {
                return balance;
            }
            index = userBalances.nextElementIndex(index);
        }
        
        // Create new balance entry
        UserBalance newBalance;
        newBalance.user = user;
        newBalance.balance = 0;
        return userBalances.add(user, newBalance, 0);
    }

public:
    REGISTER_USER_FUNCTIONS_AND_PROCEDURES
        REGISTER_USER_PROCEDURE(Deposit, 1);
        REGISTER_USER_PROCEDURE(Withdraw, 2);
        REGISTER_USER_PROCEDURE(Transfer, 3);
        REGISTER_USER_FUNCTION(GetBalance, 1);
        REGISTER_USER_PROCEDURE(AdminWithdraw, 4);
    _

    INITIALIZE
        admin = NULL_ID; // VULNERABILITY 10: Admin not properly initialized
        totalDeposits = 0;
        reentrancyLock = false;
    _
};
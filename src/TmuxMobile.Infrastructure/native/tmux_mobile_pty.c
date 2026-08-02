#include <pty.h>
#include <stdlib.h>
#include <unistd.h>

int tmux_mobile_forkpty_exec(
    int *master_fd,
    const struct winsize *window_size,
    const char *executable,
    char *const arguments[])
{
    pid_t process_id = forkpty(master_fd, NULL, NULL, window_size);
    if (process_id != 0)
        return process_id;

    execv(executable, arguments);
    _exit(127);
}
